using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace FiendGetConfigured
{
    internal sealed class TrackingState
    {
        public TrackingState() { }
        internal Component LastTarget;
        internal Vector3 LastPosition;
        internal float LastSeen;
        internal bool UsingMemory;
        internal float NextAttack = float.NegativeInfinity;
    }

    internal static class Runtime
    {
        internal static Func<Settings> SettingsProvider = delegate { return new Settings(); };
        internal static Func<float> Clock = delegate { return Time.time; };
        internal static Action<string> Warning;
        internal static Type FiendType;
        internal static Type PlayerType;
        internal static MethodInfo ChooseOriginal, NavigateOriginal, VisibilityOriginal, TargetableOriginal, GrabClient;
        internal static MethodInfo ResolveReference;
        internal static FieldInfo Target, Agent, Legacy, State, Seeking, Rage, Funky, Apparatus, Look, Breaker;
        internal static FieldInfo Dead, Controlled;
        private static PropertyInfo isServer;
        private static ConditionalWeakTable<object, TrackingState> tracking = new ConditionalWeakTable<object, TrackingState>();

        internal static Settings Current { get { return SettingsProvider(); } }

        internal static void Initialize(Type type)
        {
            if (type == null) throw new MissingMemberException("TheFiendAI was not found.");
            FiendType = type;
            Target = NeedField(type, "targetPlayer");
            PlayerType = Target.FieldType;
            Agent = NeedField(type, "agent");
            Legacy = NeedField(type, "enableLegacy");
            State = NeedField(type, "StateOfMind");
            Seeking = NeedField(type, "Seeking");
            Rage = NeedField(type, "RageMode");
            Funky = NeedField(type, "Funky");
            Apparatus = NeedField(type, "LungApparatusWillRage");
            Look = NeedField(type, "TargetLook");
            Breaker = NeedField(type, "breakerBox");
            Dead = NeedField(PlayerType, "isPlayerDead");
            Controlled = NeedField(PlayerType, "isPlayerControlled");
            ChooseOriginal = NeedMethod(type, "TargetClosestPlayer", 6);
            NavigateOriginal = NeedMethod(type, "SetDestinationToPosition", 2);
            VisibilityOriginal = NeedMethod(type, "CheckLineOfSightForPosition", 5);
            TargetableOriginal = NeedMethod(type, "PlayerIsTargetable", 4);
            GrabClient = NeedMethod(type, "GrabClientRpc", 1);
            ResolveReference = NeedMethod(GrabClient.GetParameters()[0].ParameterType, "TryGet", 2);
            isServer = AccessTools.Property(type, "IsServer");
            if (isServer == null) throw new MissingMemberException("IsServer is missing.");
            tracking = new ConditionalWeakTable<object, TrackingState>();
        }

        internal static FieldInfo NeedField(Type type, string name)
        {
            FieldInfo field = AccessTools.Field(type, name);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        internal static MethodInfo NeedMethod(Type type, string name, int parameters)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (method.Name == name && method.GetParameters().Length == parameters && !method.IsGenericMethodDefinition) return method;
            }
            throw new MissingMethodException(type.FullName, name);
        }

        internal static object NetworkValue(FieldInfo field, object instance)
        {
            object variable = field.GetValue(instance);
            if (variable == null) return null;
            return variable.GetType().GetProperty("Value").GetValue(variable, null);
        }

        internal static void SetNetworkValue(FieldInfo field, object instance, object value)
        {
            object variable = field.GetValue(instance);
            if (variable != null) variable.GetType().GetProperty("Value").SetValue(variable, value, null);
        }

        internal static bool Server(object instance) { return (bool)isServer.GetValue(instance, null); }

        internal static void Prepare(object instance)
        {
            Legacy.SetValue(instance, false);
            if (!Current.BreakerBoxDestructionEnabled) Breaker.SetValue(instance, null);
            if (Server(instance))
            {
                bool requested = Current.ApparatusRageEnabled;
                if (!object.Equals(NetworkValue(Apparatus, instance), requested)) SetNetworkValue(Apparatus, instance, requested);
            }
        }

        private static bool Valid(object instance, Component player)
        {
            return player != null && player.gameObject.activeInHierarchy && !(bool)Dead.GetValue(player)
                && (bool)Controlled.GetValue(player)
                && (bool)TargetableOriginal.Invoke(instance, new object[] { player, false, false, true });
        }

        internal static bool ChooseTarget(object instance, float buffer, bool sight, float width, bool ground, bool path, bool mineshaft)
        {
            Settings s = Current;
            TrackingState state = tracking.GetOrCreateValue(instance);
            bool found = (bool)ChooseOriginal.Invoke(instance, new object[] { s.RequireLineOfSight ? 0f : buffer,
                s.RequireLineOfSight, s.FieldOfViewDegrees / 2f, ground, path, mineshaft });
            Component player = Target.GetValue(instance) as Component;
            Component fiend = (Component)instance;
            if (found && Valid(instance, player) && Policy.IsInRange((player.transform.position - fiend.transform.position).sqrMagnitude, s.DetectionRange))
            {
                state.LastTarget = player;
                state.LastPosition = player.transform.position;
                state.LastSeen = Clock();
                state.UsingMemory = false;
                return true;
            }
            if (s.RequireLineOfSight && Valid(instance, state.LastTarget) && Policy.CanRemember(Clock(), state.LastSeen, s.TargetMemorySeconds))
            {
                Target.SetValue(instance, state.LastTarget);
                state.UsingMemory = true;
                return true;
            }
            state.LastTarget = null;
            state.UsingMemory = false;
            Target.SetValue(instance, null);
            return false;
        }

        internal static Vector3 Destination(object instance, Vector3 requested)
        {
            TrackingState state;
            if (tracking.TryGetValue(instance, out state) && state.UsingMemory && state.LastTarget != null
                && (requested - state.LastTarget.transform.position).sqrMagnitude < 0.0001f) return state.LastPosition;
            return requested;
        }

        internal static bool Navigate(object instance, Vector3 destination, bool checkPath)
        {
            return (bool)NavigateOriginal.Invoke(instance, new object[] { Destination(instance, destination), checkPath });
        }

        internal static void AfterAi(object instance)
        {
            TrackingState state;
            if (tracking.TryGetValue(instance, out state) && state.UsingMemory) Look.SetValue(instance, null);
        }

        internal static void SetSpeed(NavMeshAgent agent, float requested, object instance)
        {
            agent.speed = Policy.Speed(Current, requested, (int)NetworkValue(State, instance),
                (bool)NetworkValue(Seeking, instance), (bool)NetworkValue(Rage, instance), (int)NetworkValue(Funky, instance));
        }

        internal static bool CanContact(object instance, Component player)
        {
            if (!Valid(instance, player)) return false;
            Settings s = Current;
            if (!Policy.IsInRange((player.transform.position - ((Component)instance).transform.position).sqrMagnitude, s.AttackDistance)) return false;
            if (!s.RequireLineOfSight) return true;
            FieldInfo cameraField = AccessTools.Field(PlayerType, "gameplayCamera");
            Camera camera = cameraField == null ? null : cameraField.GetValue(player) as Camera;
            Vector3 point = camera == null ? player.transform.position + Vector3.up * 1.5f : camera.transform.position;
            return (bool)VisibilityOriginal.Invoke(instance, new object[] { point, 180f,
                Math.Max(1, Mathf.CeilToInt(s.AttackDistance + 3f)), -1f, null });
        }

        internal static bool ContactPrefix(object instance, Collider collider)
        {
            if (collider == null) return false;
            Legacy.SetValue(instance, false);
            Component player = collider.gameObject.GetComponent(PlayerType);
            return player == null || CanContact(instance, player);
        }

        internal static void SendGrab(object instance, object reference)
        {
            object[] args = new object[] { null, null };
            if (!(bool)ResolveReference.Invoke(reference, args)) return;
            Component networkObject = args[0] as Component;
            Component player = networkObject == null ? null : networkObject.GetComponent(PlayerType);
            if (!Server(instance) || !CanContact(instance, player)) return;
            TrackingState state = tracking.GetOrCreateValue(instance);
            if (!Policy.CanStartAttack(Clock(), ref state.NextAttack, Current.AttackCooldownSeconds)) return;
            GrabClient.Invoke(instance, new object[] { reference });
        }
    }
}
