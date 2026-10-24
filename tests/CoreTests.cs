using System;
using FiendGetConfigured;

internal static class CoreTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Equal(float actual, float expected, string message)
    {
        Check(Math.Abs(actual-expected) < .0001f, message + ": " + actual + " != " + expected);
    }
    private static void Main()
    {
        Settings s = new Settings();
        Equal(Policy.Speed(s,0f,2,false,true,4),0f,"attack and stun stop preserved");
        Equal(Policy.Speed(s,9f,2,false,false,1),5.5f,"normal chase");
        Equal(Policy.Speed(s,20f,2,false,true,1),7f,"rage chase");
        Equal(Policy.Speed(s,3f,1,false,false,1),3f,"walk");
        Equal(Policy.Speed(s,1f,2,true,true,1),1f,"stalk has priority");
        Equal(Policy.Speed(s,40f,2,false,true,2),7f,"disabled late boost");
        s.LateGameBoostEnabled=true;
        Equal(Policy.Speed(s,40f,2,false,true,2),14f,"configured speed multiplied");
        Equal(Policy.Speed(s,9f,2,false,false,0),5.5f,"multiplier cannot reduce below one");
        Check(Policy.CanRemember(10,5,8),"memory active");
        Check(Policy.CanRemember(13,5,8),"memory boundary");
        Check(!Policy.CanRemember(13.01f,5,8),"memory expired");
        Check(!Policy.CanRemember(10,5,0),"zero memory");
        Check(!Policy.CanRemember(3,5,8),"clock rewind does not extend memory");
        Check(Policy.IsInRange(4,2),"range boundary");
        Check(!Policy.IsInRange(4.01f,2),"outside range");
        Check(!Policy.IsInRange(float.NaN,2),"NaN range rejected");
        Check(!Policy.IsInRange(-1,2),"invalid squared distance rejected");
        float next=float.NegativeInfinity;
        Check(Policy.CanStartAttack(0,ref next,2),"first hit allowed");
        Check(!Policy.CanStartAttack(0,ref next,2),"same-tick duplicate rejected");
        Check(!Policy.CanStartAttack(1.99f,ref next,2),"early hit rejected");
        Check(Policy.CanStartAttack(2,ref next,2),"next hit allowed at boundary");
        int health=100, hits=0;
        while(health>0) { hits++; health=health<=s.ExecutionHealthThreshold ? 0 : health-s.Damage; }
        Check(hits==4,"default combat needs four accepted hits from full health");
        Check(s.RequireLineOfSight && !s.TeleportEnabled && !s.ApparatusRageEnabled,"balanced defaults");
        Console.WriteLine("CORE_TESTS_OK " + checks);
    }
}
