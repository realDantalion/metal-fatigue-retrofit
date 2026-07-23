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
        public uint Addr;   // runtime VA of the descriptor
    }

    public static class PartsData
    {
        // The four factions in UI order; Basic parts are intentionally omitted (all factions have them).
        public static readonly string[] Factions = { "Rimtech", "MilAgro", "Neuropa", "Hedoth" };
        public static readonly string[] Slots = { "Arm", "Legs", "Torso" };

        public static readonly List<PartInfo> Parts = new List<PartInfo>
        {
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "ArmorFist", Name = "ArmorFist", Cls = "CArmorFistArm", Addr = 0x5733B8u },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "EnergyBlaster", Name = "Energy Gun", Cls = "CEnergyBlasterArm", Addr = 0x573830u },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "EnergyShield", Name = "EnergyShield", Cls = "CEnergyShieldArm", Addr = 0x573A38u },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "KatanaSword", Name = "KatanaSword", Cls = "CKatanaSwordArm", Addr = 0x573898u },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "LaserSword", Name = "LaserSword", Cls = "CLaserSwordArm", Addr = 0x573900u },
            new PartInfo { Faction = "Rimtech", Slot = "Arm", Core = "LongMissile", Name = "Missile Arm", Cls = "CLongMissileArm", Addr = 0x573968u },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "BlastPulse", Name = "BlastPulse", Cls = "CBlastPulseLegs", Addr = 0x5728C0u },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "DrunkMissile", Name = "Missile", Cls = "CDrunkMissileLegs", Addr = 0x572B98u },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "HTHupgrade", Name = "HTHupgrade", Cls = "CHTHupgradeLegs", Addr = 0x572788u },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "JumpJet", Name = "Jetboots", Cls = "CJumpJetLegs", Addr = 0x572C00u },
            new PartInfo { Faction = "Rimtech", Slot = "Legs", Core = "Recon", Name = "Recon", Cls = "CReconLegs", Addr = 0x5729F8u },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "Armor", Name = "Armor", Cls = "CArmorTorso", Addr = 0x5730E0u },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "DrunkMissile", Name = "Missile", Cls = "CDrunkMissileTorso", Addr = 0x572FA8u },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "EMP", Name = "E M P", Cls = "CEMPTorso", Addr = 0x573148u },
            new PartInfo { Faction = "Rimtech", Slot = "Torso", Core = "ForceField", Name = "ForceField", Cls = "CForceFieldTorso", Addr = 0x572E08u },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "Axe", Name = "Axe", Cls = "CAxeArm", Addr = 0x5737C8u },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "BladeFist", Name = "BladeFist", Cls = "CBladeFistArm", Addr = 0x5732E8u },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "CarpetBomb", Name = "CarpetBomb", Cls = "CCarpetBombArm", Addr = 0x573760u },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "GattlingGun", Name = "Gattling", Cls = "CGattlingGunArm", Addr = 0x573628u },
            new PartInfo { Faction = "MilAgro", Slot = "Arm", Core = "HammerHand", Name = "Hammer", Cls = "CHammerHandArm", Addr = 0x5736F8u },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "HighStrength", Name = "Strength", Cls = "CHighStrengthLegs", Addr = 0x572AC8u },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "PowerGun", Name = "PowerGun", Cls = "CPowerGunLegs", Addr = 0x572B30u },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "PowerShield", Name = "Power Shield", Cls = "CPowerShieldLegs", Addr = 0x5727F0u },
            new PartInfo { Faction = "MilAgro", Slot = "Legs", Core = "Steady", Name = "Steady", Cls = "CSteadyLegs", Addr = 0x572CD0u },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Flak", Name = "Flak", Cls = "CFlakTorso", Addr = 0x572DA0u },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Howitzer", Name = "Howitzer", Cls = "CHowitzerTorso", Addr = 0x573010u },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "JetPack", Name = "JetPack", Cls = "CJetPackTorso", Addr = 0x5731B0u },
            new PartInfo { Faction = "MilAgro", Slot = "Torso", Core = "Recon", Name = "Recon", Cls = "CReconTorso", Addr = 0x572ED8u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "ElectroGrip", Name = "ElectroGrip", Cls = "CElectroGripArm", Addr = 0x5735C0u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "HomingMissiles", Name = "Homing", Cls = "CHomingMissilesArm", Addr = 0x5734F0u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "PlasmaCannon", Name = "Plasma Gun", Cls = "CPlasmaCannonArm", Addr = 0x573488u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "PowerFist", Name = "PowerFist", Cls = "CPowerFistArm", Addr = 0x573350u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "RotaryBlade", Name = "Electroblade", Cls = "CRotaryBladeArm", Addr = 0x573420u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "Shield", Name = "K-Shield", Cls = "CShieldArm", Addr = 0x573690u },
            new PartInfo { Faction = "Neuropa", Slot = "Arm", Core = "SniperLaser", Name = "Sniper Beam", Cls = "CSniperLaserArm", Addr = 0x573558u },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Armor", Name = "Armor", Cls = "CArmorLegs", Addr = 0x572A60u },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "HighSpeed", Name = "Speed", Cls = "CHighSpeedLegs", Addr = 0x572990u },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Laser", Name = "Laser", Cls = "CLaserLegs", Addr = 0x572C68u },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "PowerPulse", Name = "Power Pulse", Cls = "CPowerPulseLegs", Addr = 0x572858u },
            new PartInfo { Faction = "Neuropa", Slot = "Legs", Core = "Sonar", Name = "Sonar", Cls = "CSonarLegs", Addr = 0x572720u },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "GRPCammo", Name = "AreaCloak", Cls = "CGRPCammoTorso", Addr = 0x572E70u },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "JetPack", Name = "JetPack", Cls = "CJetPackTorso", Addr = 0x5731B0u },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "SelfRepair", Name = "SelfRepair", Cls = "CSelfRepairTorso", Addr = 0x572F40u },
            new PartInfo { Faction = "Neuropa", Slot = "Torso", Core = "TracerFire", Name = "TracerFire", Cls = "CTracerFireTorso", Addr = 0x573078u },
            new PartInfo { Faction = "Hedoth", Slot = "Arm", Core = "MultiClaw", Name = "MultiClaw", Cls = "CMultiClawArm", Addr = 0x573BD8u },
            new PartInfo { Faction = "Hedoth", Slot = "Arm", Core = "ProtoBlast", Name = "ProtoBlast", Cls = "CProtoBlastArm", Addr = 0x573C40u },
            new PartInfo { Faction = "Hedoth", Slot = "Legs", Core = "AlienGeneric", Name = "Alien", Cls = "CAlienGenericLegs", Addr = 0x573218u },
            new PartInfo { Faction = "Hedoth", Slot = "Torso", Core = "AlienGeneric", Name = "Alien", Cls = "CAlienGenericTorso", Addr = 0x572D38u },
        };

        public sealed class SuperWeapon { public string Name, Cls; public uint Addr; }

        // Only the three faction-specific superweapons are offered. Imaging Pole, Orbital Bomb and
        // Tectonic Torpedo are buildable by every faction already, so unlocking them is pointless
        // (same reasoning as excluding the Basic parts).
        public static readonly List<SuperWeapon> Superweapons = new List<SuperWeapon>
        {
            new SuperWeapon { Name = "Neutron Bomb (Rimtech)",  Cls = "CNeutronBombTurret", Addr = 0x5720A0u },
            new SuperWeapon { Name = "EMP Shell (Mil-Agro)",    Cls = "CEMPTurret",         Addr = 0x572038u },
            new SuperWeapon { Name = "Phase Charge (Neuropa)",  Cls = "CPhaseChargeTurret", Addr = 0x571F00u },
        };
    }
}
