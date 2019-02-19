
namespace PZ_BF4
{
    public struct Offsets
    {
        public static ulong OFFSET_GAMERENDERER = 0x1439e6d08; //0x1424ad330;

        public struct PZ_GameRenderer
        {
            public static ulong m_pRenderView = 0x60; // RenderView
        }

        public struct PZ_RenderView
        {
            public static ulong m_viewProj = 0x460;          // D3DXMATRIX
        }

        public struct PZ_DynamicPhysicsEntity
        {
            public static ulong m_EntityTransform = 0xA0;  // PhysicsEntityTransform
        }

        public struct PZ_PhysicsEntityTransform
        {
            public static ulong m_Transform;       // D3DXMATRIX
        }

        public struct PZ_VehicleEntityData
        {
            public static ulong m_FrontMaxHealth = 0x1F8; // FLOAT
            public static ulong m_NameSid = 0x2F8;       // char* ID_P_VNAME_9K22
        }

        public struct PZ_ClientSoldierEntity
        {
            public static ulong m_data = 0x30;         // VehicleEntityData
            public static ulong m_pPlayer = 0x0;          // ClientPlayer
            public static ulong m_pHealthComponent = 0x1D0; // HealthComponent
            public static ulong m_authorativeYaw = 0x604;   // FLOAT
            public static ulong m_authorativePitch = 0x634; // FLOAT 
            public static ulong m_poseType = 0x638;         // INT32
            public static ulong m_pPredictedController = 0x5E0;    // ClientSoldierPrediction
            public static ulong m_ragdollComponent = 0x490;        // ClientRagDollComponent 
            public static ulong m_occluded = 0x6EB;  // BYTE
        }

        public struct PZ_HealthComponent
        {
            public static ulong m_Health = 0x20;        // FLOAT
            public static ulong m_MaxHealth = 0x24;     // FLOAT
            public static ulong m_vehicleHealth = 0x40; // FLOAT (pLocalSoldier + 0x1E0 + 0x14C0 + 0x140 + 0x38)
        }

        public struct PZ_ClientSoldierPrediction
        {
            public static ulong m_Position = 0x10; // D3DXVECTOR3
        }


        public struct PZ_ClientPlayer
        {
            public static ulong szName = 0x40;            // 10 CHARS
            public static ulong m_teamId = 0x1C34;        // INT32
            public static ulong m_character = 0x1D30;     // ClientSoldierEntity 
            public static ulong m_pAttachedControllable = 0x1D38;   // ClientSoldierEntity (ClientVehicleEntity)
            public static ulong m_pControlledControllable = 0x1D48; // ClientSoldierEntity
            public static ulong m_attachedEntryId = 0x1D40; // INT32
            public static ulong m_isSpectator = 0x1C31;
        }

        public struct PZ_ClientVehicleEntity
        {
            public static ulong m_pPhysicsEntity = 0x238; // DynamicPhysicsEntity
            public static ulong m_childrenAABB = 0x2E0;   // AxisAlignedBox
        }


        public struct PZ_UpdatePoseResultData
        {
            public enum BONES
            {
                BONE_HEAD = 53,
                BONE_NECK = 51,
                BONE_SPINE2 = 7,
                BONE_SPINE1 = 6,
                BONE_SPINE = 5,
                BONE_LEFTSHOULDER = 8,
                BONE_RIGHTSHOULDER = 163,
                BONE_LEFTELBOWROLL = 14,
                BONE_RIGHTELBOWROLL = 169,
                BONE_LEFTHAND = 16,
                BONE_RIGHTHAND = 171,
                BONE_LEFTKNEEROLL = 285,
                BONE_RIGHTKNEEROLL = 299,
                BONE_LEFTFOOT = 277,
                BONE_RIGHTFOOT = 291
            };

            public static ulong m_ActiveWorldTransforms = 0x20; // QuatTransform
            public static ulong m_ValidTransforms = 0x38;       // BYTE
        }

        public struct PZ_ClientRagDollComponent
        {
            public static ulong m_ragdollTransforms = 0x88; // UpdatePoseResultData
            public static ulong m_Transform = 0x5D0;         // D3DXMATRIX
        }

    }
}
