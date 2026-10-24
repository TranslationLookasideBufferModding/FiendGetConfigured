using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public static class FiendRuntimeTests
{
    private static Type runtime, settingsType, patches;
    private static object settings, harmony;
    private static Type harmonyType;
    private static int checks;
    private static float clock = 1f;
    private static readonly List<GameObject> objects = new List<GameObject>();
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    public static void Run()
    {
        string[] args=Environment.GetCommandLineArgs();
        string pluginPath=args[Array.IndexOf(args,"-pluginPath")+1];
        string corePath=args[Array.IndexOf(args,"-corePath")+1];
        string managedPath=args[Array.IndexOf(args,"-managedPath")+1];
        string fiendPath=args[Array.IndexOf(args,"-fiendPath")+1];
        string lethalLibPath=args[Array.IndexOf(args,"-lethalLibPath")+1];
        AppDomain.CurrentDomain.AssemblyResolve += (sender,e) => {
            string file=Path.Combine(corePath,new AssemblyName(e.Name).Name+".dll");
            if(File.Exists(file))return Assembly.LoadFrom(file);
            file=Path.Combine(managedPath,new AssemblyName(e.Name).Name+".dll");
            if(File.Exists(file))return Assembly.LoadFrom(file);
            return new AssemblyName(e.Name).Name=="LethalLib"?Assembly.LoadFrom(lethalLibPath):null;
        };
        Assembly addon=Assembly.LoadFrom(pluginPath);
        runtime=addon.GetType("FiendGetConfigured.Runtime",true);
        settingsType=addon.GetType("FiendGetConfigured.Settings",true);
        patches=addon.GetType("FiendGetConfigured.Patches",true);
        settings=Activator.CreateInstance(settingsType,true);
        Type providerType=typeof(Func<>).MakeGenericType(settingsType);
        Delegate provider=Expression.Lambda(providerType,Expression.Constant(settings,settingsType)).Compile();
        runtime.GetField("SettingsProvider",Static).SetValue(null,provider);
        runtime.GetField("Clock",Static).SetValue(null,new Func<float>(()=>clock));
        Call("Initialize",typeof(FixtureFiend));
        harmonyType=Assembly.LoadFrom(Path.Combine(corePath,"0Harmony.dll")).GetType("HarmonyLib.Harmony",true);
        harmony=Activator.CreateInstance(harmonyType,new object[]{"FiendGetConfigured.RuntimeTests"});
        patches.GetMethod("Install",Static).Invoke(null,new object[]{harmony,false});
        try
        {
            FixtureFiend fiend=MakeFiend();
            FixturePlayer player=MakePlayer();
            fiend.Candidate=player;
            player.transform.position=new Vector3(5,0,0);
            Check(!fiend.enableLegacy,"Awake uses modern behavior");
            fiend.OnNetworkSpawn();
            Check(!fiend.enableLegacy && !fiend.LungApparatusWillRage.Value,"network spawn overrides legacy and apparatus setting");
            fiend.DoAIInterval();
            Check(fiend.targetPlayer==player,"visible player acquired");
            Near(fiend.agent.speed,5.5f,"patched chase speed");
            Near(fiend.LastDestination.x,5f,"visible destination");
            Check(fiend.LastRequireSight,"visibility is passed to original selector");
            Near(fiend.LastWidth,70f,"full FOV converted to half-angle");
            Check(fiend.ApparatusAiCalls==0,"original apparatus flag cannot override addon");
            fiend.CanSee=false;
            player.transform.position=new Vector3(12,0,0);
            clock=3f;
            fiend.DoAIInterval();
            Check(fiend.targetPlayer==player,"last target remembered");
            Near(fiend.LastDestination.x,5f,"last seen position rather than moving hidden player");
            Check(fiend.TargetLook==null,"head does not track hidden player");
            clock=10f;
            fiend.DoAIInterval();
            Check(fiend.targetPlayer==null,"target forgotten after memory duration");
            fiend.CanSee=true;
            player.transform.position=new Vector3(31,0,0);
            fiend.DoAIInterval();
            Check(fiend.targetPlayer==null,"detection range enforced");
            Set("RequireLineOfSight",false);
            fiend.CanSee=false;
            player.transform.position=new Vector3(8,0,0);
            fiend.DoAIInterval();
            Check(fiend.targetPlayer==player && !fiend.LastRequireSight,"configurable original-style acquisition");
            Near(fiend.LastDestination.x,8f,"visible memory bypass when sight not required");
            Set("RequireLineOfSight",true);
            fiend.CanSee=true;
            fiend.timeOfDay.hour=20;
            fiend.Funky.Value=1;
            fiend.DoAIInterval();
            Check(fiend.Funky.Value==1,"late boost disabled");
            Set("LateGameBoostEnabled",true);Set("LateGameMultiplier",3);
            fiend.DoAIInterval();
            Check(fiend.Funky.Value==3,"configured late factor");
            Near(fiend.agent.speed,16.5f,"late factor applied to configured speed");
            Set("LateGameBoostEnabled",false);
            fiend.RageMode.Value=true;
            fiend.DoAIInterval();
            Near(fiend.agent.speed,7f,"rage speed without late boost");
            fiend.RageMode.Value=false;
            player.transform.position=new Vector3(1,0,0);
            fiend.CanSee=true;
            player.health=100;player.isPlayerDead=false;
            clock=30;
            FixtureReference reference=new FixtureReference(player);
            fiend.GrabServerRpc(reference);
            Check(player.health==75,"configured first damage");
            Near(fiend.LastCooldown,2f,"configured coroutine cooldown");
            Near(player.externalForceAutoFade.magnitude,15f,"configured normal-hit knockback");
            fiend.GrabServerRpc(reference);
            Check(player.health==75 && fiend.AcceptedGrabs==1,"duplicate RPC same tick rejected");
            clock=31.99f;fiend.GrabServerRpc(reference);
            Check(player.health==75,"cooldown enforced on server");
            clock=32;fiend.GrabServerRpc(reference);
            clock=34;fiend.GrabServerRpc(reference);
            Check(player.health==25 && player.Executions==0,"three hits leave 25 HP");
            clock=36;fiend.GrabServerRpc(reference);
            Check(player.health==0 && player.Executions==1 && fiend.AcceptedGrabs==4,"fourth accepted hit executes");
            player.health=100;player.isPlayerDead=false;
            player.transform.position=new Vector3(3,0,0);
            clock=40;fiend.GrabServerRpc(reference);
            Check(player.health==100,"server rejects contact outside configured range");
            fiend.OnTriggerStay(player.GetComponent<Collider>());
            Check(fiend.ContactCalls==0,"contact distance transpiler");
            player.transform.position=new Vector3(1,0,0);
            fiend.CanSee=false;fiend.GrabServerRpc(reference);
            Check(player.health==100,"server rejects invisible contact");
            fiend.OnTriggerStay(player.GetComponent<Collider>());
            Check(fiend.ContactCalls==0,"contact prefix requires visibility");
            fiend.CanSee=true;fiend.OnTriggerStay(player.GetComponent<Collider>());
            Check(fiend.ContactCalls==1,"valid contact preserved");
            player.isPlayerControlled=false;fiend.GrabServerRpc(reference);
            Check(player.health==100,"uncontrolled player cannot be attacked");
            player.isPlayerControlled=true;
            Set("ExecutionHealthThreshold",-1);
            player.health=20;player.Executions=0;
            clock=44;fiend.GrabServerRpc(reference);
            Check(player.health<=0 && player.Executions==0,"execution disabled while normal damage can kill");
            player.health=100;player.isPlayerDead=false;
            Set("Damage",10);Set("AttackCooldownSeconds",1.5f);Set("KnockbackForce",9f);
            player.externalForceAutoFade=Vector3.zero;
            clock=48;fiend.GrabServerRpc(reference);
            Check(player.health==90,"custom damage");
            Near(fiend.LastCooldown,1.5f,"custom cooldown");
            Near(player.externalForceAutoFade.magnitude,9f,"custom knockback");
            fiend.TeleportServerRpc();Check(fiend.Teleports==0,"teleport disabled");
            Set("TeleportEnabled",true);fiend.TeleportServerRpc();Check(fiend.Teleports==1,"teleport enabled");
            fiend.SetLungLightServerRpc();Check(fiend.ApparatusCalls==0,"apparatus rage disabled");
            Set("ApparatusRageEnabled",true);fiend.SetLungLightServerRpc();fiend.OnNetworkSpawn();
            Check(fiend.ApparatusCalls==1 && fiend.LungApparatusWillRage.Value,"apparatus rage enabled");
            fiend.BreakDoorServerRpc();Check(fiend.DoorCalls==1,"doors enabled");
            Set("BreakDoorsEnabled",false);fiend.BreakDoorServerRpc();Check(fiend.DoorCalls==1,"doors disabled");
            Set("BreakerBoxDestructionEnabled",false);
            fiend.breakerBox=MakeObject("breaker");fiend.OnNetworkSpawn();fiend.BreakerBoxBreakServerRpc();
            Check(fiend.breakerBox==null && fiend.BreakerCalls==0,"breaker target and destruction disabled together");
            Set("BreakerBoxDestructionEnabled",true);fiend.BreakerBoxBreakServerRpc();Check(fiend.BreakerCalls==1,"breaker enabled");
            fiend.FearedServerRpc(true,false);Check(!fiend.LastTemporaryRage,"temporary rage disabled");
            Set("TemporaryRageEnabled",true);fiend.FearedServerRpc(true,false);Check(fiend.LastTemporaryRage,"temporary rage enabled");
            Check(fiend.FearCalls==2,"fear response itself retained");
            player.health=100;player.isPlayerDead=false;fiend.CanSee=true;clock=55;fiend.DoAIInterval();
            player.isInsideFactory=false;fiend.CanSee=false;clock=56;fiend.DoAIInterval();
            Check(fiend.targetPlayer==null,"memory does not follow player outside valid area");
            harmonyType.GetMethod("UnpatchSelf").Invoke(harmony,null);
            bool rejected=false;
            try { patches.GetMethod("Install",Static).Invoke(null,new object[]{harmony,true}); }
            catch(TargetInvocationException e) { rejected=e.InnerException is InvalidOperationException; }
            Check(rejected,"strict production layout rejects modified fixture before patching");
            FixtureFiend unpatched=MakeFiend();Check(unpatched.enableLegacy,"failed validation leaves original behavior intact");
            Type installed=Assembly.LoadFrom(fiendPath).GetType("TheFiendAI",true);
            Call("Initialize",installed);
            patches.GetMethod("Validate",Static).Invoke(null,new object[]{true});
            checks++;
            Debug.Log("REAL_FIEND_PREFLIGHT_OK AuroraBytes 1.1.6; all exact instruction patterns validated");
            patches.GetMethod("Install",Static).Invoke(null,new object[]{harmony,true});
            checks++;
            Debug.Log("REAL_FIEND_PATCH_INSTALL_OK all target methods patched and restored without spawning game entities");
            harmonyType.GetMethod("UnpatchSelf").Invoke(harmony,null);
            Debug.Log("RUNTIME_TESTS_OK "+checks+" (real Harmony and Unity; synthetic Fiend fixture, not a multiplayer game test)");
        }
        finally
        {
            harmonyType.GetMethod("UnpatchSelf").Invoke(harmony,null);
            foreach(GameObject obj in objects)if(obj)UnityEngine.Object.DestroyImmediate(obj);
        }
    }
    private static void Set(string name,object value){settingsType.GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(settings,value);}
    private static object Call(string name,params object[] args){return runtime.GetMethod(name,Static).Invoke(null,args);}
    private static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL "+name);checks++;}
    private static void Near(float actual,float expected,string name){Check(Mathf.Abs(actual-expected)<.001f,name+": "+actual+" != "+expected);}
    private static GameObject MakeObject(string name){GameObject obj=new GameObject(name);objects.Add(obj);return obj;}
    private static FixtureFiend MakeFiend(){GameObject obj=MakeObject("Fiend fixture");obj.AddComponent<NavMeshAgent>();FixtureFiend fiend=obj.AddComponent<FixtureFiend>();fiend.IsServer=true;fiend.Main=obj;fiend.agent=obj.GetComponent<NavMeshAgent>();fiend.Awake();return fiend;}
    private static FixturePlayer MakePlayer(){GameObject obj=MakeObject("Player fixture");obj.AddComponent<BoxCollider>();return obj.AddComponent<FixturePlayer>();}
}

public sealed class FixtureValue<T>{public T Value{get;set;}}
public sealed class FixtureTime{public int hour;}
public static class FixtureOriginal{public static bool EnableLegacyBehaviourConfig=true;public static bool WillRageAfterApparatusConfig=true;}
public sealed class FixturePlayer:MonoBehaviour
{
    public int health=100,Executions;
    public bool isPlayerDead,isPlayerControlled=true,isInsideFactory=true;
    public Camera gameplayCamera;
    public Vector3 externalForceAutoFade;
    public void DamagePlayer(int amount){health-=amount;if(health<=0)isPlayerDead=true;}
}
public struct FixtureReference
{
    private readonly FixturePlayer player;
    public FixtureReference(FixturePlayer value){player=value;}
    public bool TryGet(out FixturePlayer value,object manager=null){value=player;return value!=null;}
}
public sealed class FixtureFiend:MonoBehaviour
{
    public FixturePlayer targetPlayer,Candidate;
    public NavMeshAgent agent;
    public bool enableLegacy,CanSee=true;
    public bool IsServer{get;set;}
    public FixtureValue<int> StateOfMind=new FixtureValue<int>(),Funky=new FixtureValue<int>{Value=1};
    public FixtureValue<bool> Seeking=new FixtureValue<bool>(),RageMode=new FixtureValue<bool>(),LungApparatusWillRage=new FixtureValue<bool>();
    public GameObject TargetLook,breakerBox,Main;
    public FixtureTime timeOfDay=new FixtureTime();
    public Vector3 LastDestination;
    public bool LastRequireSight,LastTemporaryRage;
    public float LastWidth,LastCooldown;
    public int AcceptedGrabs,ContactCalls,Teleports,ApparatusCalls,ApparatusAiCalls,DoorCalls,BreakerCalls,FearCalls;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Awake(){enableLegacy=FixtureOriginal.EnableLegacyBehaviourConfig;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnNetworkSpawn(){enableLegacy=FixtureOriginal.EnableLegacyBehaviourConfig;}
    public bool PlayerIsTargetable(FixturePlayer player,bool ship,bool inside,bool mineshaft){return player!=null && player.isInsideFactory && player.isPlayerControlled && !player.isPlayerDead;}
    public bool TargetClosestPlayer(float buffer,bool sight,float width,bool ground,bool path,bool mineshaft)
    {LastRequireSight=sight;LastWidth=width;targetPlayer=(!sight||CanSee)?Candidate:null;return targetPlayer!=null;}
    public bool CheckLineOfSightForPosition(Vector3 point,float width,int range,float proximity,Transform eye){return CanSee;}
    public bool SetDestinationToPosition(Vector3 point,bool check){LastDestination=point;return true;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DoAIInterval()
    {
        if(timeOfDay.hour>=15)Funky.Value=2;
        if(FixtureOriginal.WillRageAfterApparatusConfig)ApparatusAiCalls++;
        if(!TargetClosestPlayer(100f,false,70f,false,false,true))return;
        TargetLook=targetPlayer.gameObject;
        StateOfMind.Value=2;
        agent.speed=9f*Funky.Value;
        SetDestinationToPosition(targetPlayer.transform.position,false);
    }
    public IEnumerator Grabbing(FixturePlayer player)
    {
        if(player.health<=50){player.Executions++;player.health=0;player.isPlayerDead=true;}
        else{player.DamagePlayer(50);player.externalForceAutoFade+=Main.transform.forward*30f;StartCooldown(1f);}
        yield return null;
        GC.KeepAlive(player);
    }
    public void StartCooldown(float seconds,bool unInvis=false){LastCooldown=seconds;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void GrabServerRpc(FixtureReference reference){GrabClientRpc(reference);}
    public void GrabClientRpc(FixtureReference reference){FixturePlayer player;reference.TryGet(out player);AcceptedGrabs++;Grabbing(player).MoveNext();}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void OnTriggerStay(Collider collider){if(Vector3.Distance(transform.position,collider.transform.position)<4f)ContactCalls++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void TeleportServerRpc(){Teleports++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetLungLightServerRpc(){ApparatusCalls++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BreakDoorServerRpc(){DoorCalls++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BreakerBoxBreakServerRpc(){BreakerCalls++;}
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void FearedServerRpc(bool temp,bool light){LastTemporaryRage=temp;FearCalls++;}
}
