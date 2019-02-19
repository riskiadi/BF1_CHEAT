
using System.Drawing.Drawing2D;
using System.Numerics;

namespace PZ_BF4
{
    class GPlayer
    {
        public string Name;
        public string VehicleName;
        public uint Team;
        public Vector3 Origin;
        public RDoll Bone;
        public bool IsFriendly;
        public uint Pose;
        public ulong pSoldier;
        public bool IsSpectator;

        public uint IsOccluded;
        public bool IsDriver;
        public bool InVehicle;

        public float Health;
        public float MaxHealth;


        public Matrix4x4 ViewProj;

        public float Yaw;
        public float Distance;

        // Vehicle
        public AxisAlignedBox VehicleAABB;
        public Matrix4x4 VehicleTranfsorm;
        public float VehicleHealth;
        public float VehicleMaxHealth;


        public bool IsValid()
        {
            return (Health > 0.1f && Health <= 100 );
        }

        public bool IsDead()
        {
            return !(Health > 0.1f);
        }


        public bool IsVisible()
        {
            return (IsOccluded == 0);
        }

        public AxisAlignedBox GetAABB()
        {
            AxisAlignedBox aabb = new AxisAlignedBox();
            if (this.Pose == 0) // standing
            {
                aabb.Min = new Vector4(-0.350000f, 0.000000f, -0.350000f, 0);
                aabb.Max = new Vector4(0.350000f, 1.700000f, 0.350000f, 0);
            }
            if (this.Pose == 1) // crouching
            {
                aabb.Min = new Vector4(-0.350000f, 0.000000f, -0.350000f, 0);
                aabb.Max = new Vector4(0.350000f, 1.150000f, 0.350000f, 0);
            }
            if (this.Pose == 2) // prone
            {
                aabb.Min = new Vector4(-0.350000f, 0.000000f, -0.350000f, 0);
                aabb.Max = new Vector4(0.350000f, 0.400000f, 0.350000f,0);
            }
            return aabb;
        }
    }
}
