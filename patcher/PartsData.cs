// Metal Fatigue Retrofit - combot part + superweapon unlock data
// Generated from research/parts_unlock_data.json. NOTE: the superweapon list below is manually
// curated to the three faction-specific turrets (the other three are buildable by every faction);
// preserve that if you regenerate.
// Descriptor addresses are VA (image base 0x400000, non-relocated .data). The build-list
// gate at fileoff 0x4bade9 reads the availability mask at descriptor+0x4c; ORing the local
// player bit there unlocks the part (with icon). See docs/plan-2.0.md.
using System.Collections.Generic;

namespace MetalFatiguePatcher
{
    public sealed class PartInfo
    {
        public string Faction, Slot, Core, Name, Cls;
        public int IconIndex;   // index into GameIcons.Faction(Faction); -1 = no icon
        public uint Addr;   // runtime VA of the descriptor
    }

    public static class PartsData
    {
        // The four factions in UI order; Basic parts are intentionally omitted (all factions have them).
        public static readonly string[] Factions = { "Rimtech", "MilAgro", "Neuropa", "Hedoth" };
        public static readonly string[] Slots = { "Arm", "Legs", "Torso" };

        public static readonly List<PartInfo> Parts = new List<PartInfo>
        {
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "ArmorFist", Name = "ArmorFist", Cls = "CArmorFistArm", Addr = 0x5733B8u, IconIndex = 25 },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "EnergyBlaster", Name = "Energy Gun", Cls = "CEnergyBlasterArm", Addr = 0x573830u, IconIndex = 13 },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "EnergyShield", Name = "EnergyShield", Cls = "CEnergyShieldArm", Addr = 0x573A38u, IconIndex = 8 },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "KatanaSword", Name = "KatanaSword", Cls = "CKatanaSwordArm", Addr = 0x573898u, IconIndex = 12 },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "LaserSword", Name = "LaserSword", Cls = "CLaserSwordArm", Addr = 0x573900u, IconIndex = 11 },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "LongMissile", Name = "Missile Arm", Cls = "CLongMissileArm", Addr = 0x573968u, IconIndex = 10 },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "BlastPulse", Name = "BlastPulse", Cls = "CBlastPulseLegs", Addr = 0x5728C0u, IconIndex = 49 },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "DrunkMissile", Name = "Missile", Cls = "CDrunkMissileLegs", Addr = 0x572B98u, IconIndex = 42 },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "HTHupgrade", Name = "HTHupgrade", Cls = "CHTHupgradeLegs", Addr = 0x572788u, IconIndex = 52 },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "JumpJet", Name = "Jetboots", Cls = "CJumpJetLegs", Addr = 0x572C00u, IconIndex = 41 },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "Recon", Name = "Recon", Cls = "CReconLegs", Addr = 0x5729F8u, IconIndex = 46 },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "Armor", Name = "Armor", Cls = "CArmorTorso", Addr = 0x5730E0u, IconIndex = 30 },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "DrunkMissile", Name = "Missile", Cls = "CDrunkMissileTorso", Addr = 0x572FA8u, IconIndex = 33 },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "EMP", Name = "E M P", Cls = "CEMPTorso", Addr = 0x573148u, IconIndex = 29 },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "ForceField", Name = "ForceField", Cls = "CForceFieldTorso", Addr = 0x572E08u, IconIndex = 37 },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "Axe", Name = "Axe", Cls = "CAxeArm", Addr = 0x5737C8u, IconIndex = 14 },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "BladeFist", Name = "BladeFist", Cls = "CBladeFistArm", Addr = 0x5732E8u, IconIndex = 15 },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "CarpetBomb", Name = "CarpetBomb", Cls = "CCarpetBombArm", Addr = 0x573760u, IconIndex = 16 },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "GattlingGun", Name = "Gattling", Cls = "CGattlingGunArm", Addr = 0x573628u, IconIndex = 19 },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "HammerHand", Name = "Hammer", Cls = "CHammerHandArm", Addr = 0x5736F8u, IconIndex = 17 },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "HighStrength", Name = "Strength", Cls = "CHighStrengthLegs", Addr = 0x572AC8u, IconIndex = 44 },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "PowerGun", Name = "PowerGun", Cls = "CPowerGunLegs", Addr = 0x572B30u, IconIndex = 43 },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "PowerShield", Name = "Power Shield", Cls = "CPowerShieldLegs", Addr = 0x5727F0u, IconIndex = 51 },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "Steady", Name = "Steady", Cls = "CSteadyLegs", Addr = 0x572CD0u, IconIndex = 39 },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Flak", Name = "Flak", Cls = "CFlakTorso", Addr = 0x572DA0u, IconIndex = 38 },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Howitzer", Name = "Howitzer", Cls = "CHowitzerTorso", Addr = 0x573010u, IconIndex = 32 },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "JetPack", Name = "JetPack", Cls = "CJetPackTorso", Addr = 0x5731B0u, IconIndex = 28 },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Recon", Name = "Recon", Cls = "CReconTorso", Addr = 0x572ED8u, IconIndex = 35 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "ElectroGrip", Name = "ElectroGrip", Cls = "CElectroGripArm", Addr = 0x5735C0u, IconIndex = 20 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "HomingMissiles", Name = "Homing", Cls = "CHomingMissilesArm", Addr = 0x5734F0u, IconIndex = 22 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "PlasmaCannon", Name = "Plasma Gun", Cls = "CPlasmaCannonArm", Addr = 0x573488u, IconIndex = 23 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "PowerFist", Name = "PowerFist", Cls = "CPowerFistArm", Addr = 0x573350u, IconIndex = 26 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "RotaryBlade", Name = "Electroblade", Cls = "CRotaryBladeArm", Addr = 0x573420u, IconIndex = 24 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "Shield", Name = "K-Shield", Cls = "CShieldArm", Addr = 0x573690u, IconIndex = 18 },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "SniperLaser", Name = "Sniper Beam", Cls = "CSniperLaserArm", Addr = 0x573558u, IconIndex = 21 },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Armor", Name = "Armor", Cls = "CArmorLegs", Addr = 0x572A60u, IconIndex = 45 },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "HighSpeed", Name = "Speed", Cls = "CHighSpeedLegs", Addr = 0x572990u, IconIndex = 47 },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Laser", Name = "Laser", Cls = "CLaserLegs", Addr = 0x572C68u, IconIndex = 40 },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "PowerPulse", Name = "Power Pulse", Cls = "CPowerPulseLegs", Addr = 0x572858u, IconIndex = 50 },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Sonar", Name = "Sonar", Cls = "CSonarLegs", Addr = 0x572720u, IconIndex = 53 },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "GRPCammo", Name = "AreaCloak", Cls = "CGRPCammoTorso", Addr = 0x572E70u, IconIndex = 36 },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "JetPack", Name = "JetPack", Cls = "CJetPackTorso", Addr = 0x5731B0u, IconIndex = 28 },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "SelfRepair", Name = "SelfRepair", Cls = "CSelfRepairTorso", Addr = 0x572F40u, IconIndex = 34 },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "TracerFire", Name = "TracerFire", Cls = "CTracerFireTorso", Addr = 0x573078u, IconIndex = 31 },
            new PartInfo { Faction = "Hedoth", Slot = "Arm", Core = "MultiClaw", Name = "MultiClaw", Cls = "CMultiClawArm", Addr = 0x573BD8u, IconIndex = 6 },
            new PartInfo { Faction = "Hedoth", Slot = "Arm", Core = "ProtoBlast", Name = "ProtoBlast", Cls = "CProtoBlastArm", Addr = 0x573C40u, IconIndex = 7 },
            new PartInfo { Faction = "Hedoth", Slot = "Legs", Core = "AlienGeneric", Name = "Alien", Cls = "CAlienGenericLegs", Addr = 0x572D38u, IconIndex = 4 },
            new PartInfo { Faction = "Hedoth", Slot = "Torso", Core = "AlienGeneric", Name = "Alien", Cls = "CAlienGenericTorso", Addr = 0x573218u, IconIndex = 5 },
        };

        public sealed class SuperWeapon { public string Name, Cls, Faction; public uint Addr; public int IconIndex; }

        // Only the three faction-specific superweapons are offered. Imaging Pole, Orbital Bomb and
        // Tectonic Torpedo are buildable by every faction already, so unlocking them is pointless
        // (same reasoning as excluding the Basic parts).
        public static readonly List<SuperWeapon> Superweapons = new List<SuperWeapon>
        {
            new SuperWeapon { Name = "Neutron Bomb (Rimtech)",  Cls = "CNeutronBombTurret", Addr = 0x5720A0u, Faction = "Rimtech", IconIndex = 72 },
            new SuperWeapon { Name = "EMP Shell (Mil-Agro)",    Cls = "CEMPTurret",         Addr = 0x572038u, Faction = "MilAgro", IconIndex = 73 },
            new SuperWeapon { Name = "Phase Charge (Neuropa)",  Cls = "CPhaseChargeTurret", Addr = 0x571F00u, Faction = "Neuropa", IconIndex = 76 },
        };
    }
}
