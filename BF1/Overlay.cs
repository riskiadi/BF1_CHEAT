using System;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Numerics;
using System.IO.MemoryMappedFiles;

namespace PZ_BF4
{
    public class Overlay
    {
        
        private Random rnd = new Random();
        // Game Data
        private static GPlayer localPlayer = null;
        private static List<GPlayer> players = null;
        private int spectatorCount = 0;

        // Screen Size
        private Rectangle rect;

        #region MAIN : Overlay

        Charm.RPM bRPM;
        Charm.Renderer bRANDER;
        // Init
        public Overlay()
        {


            // Init player array
            localPlayer = new GPlayer();
            players = new List<GPlayer>();

            // initialize a new Charm instance
            Charm charm = new Charm();

            // Make the overlay only render when the taget's window is active, and draw the fps
            charm.CharmSetOptions(Charm.CharmSettings.CHARM_REQUIRE_FOREGROUND |Charm.CharmSettings.CHARM_VSYNC |Charm.CharmSettings.CHARM_DRAW_FPS);

            // initialize the overlay, with our callback function above, and the name of the game to adhere to
            charm.CharmInit(DrawLoop, "bf1");

            // Init Key Listener
            KeyAssign();
        }

        // Update Thread
        private void DrawLoop(Charm.RPM rpm, Charm.Renderer renderer, int width, int height)
        {
            bRPM = rpm;
            bRANDER = renderer;
            rect.Width = width;
            rect.Height = height;
            OverlayControl();
        }
        private void OverlayControl()
        {
                    #region Scan Memory & Draw Players
                    MainScan();
                    #endregion

                    #region Drawing Menu
                    if (bMenuControl)
                        DrawMenu(rect.Left + 5, rect.Bottom / 4 + 5);
            #endregion

            if (bCrosshair)
            {
                bRANDER.DrawCrosshair(Charm.CrosshairStyle.Dot, rect.Width / 2, rect.Height / 2 + 2, 2, 5, Color.Red);
                bRANDER.DrawCrosshair(Charm.CrosshairStyle.Gap, rect.Width / 2, rect.Height / 2 + 2, 8, 5, Color.OrangeRed);
            }

        }

        #endregion

        #region Scan Game Memory Stuff

        private bool bBoneOk = false;

        private ulong PegaClienteSoldadodEntidade(ulong pClientPlayer, GPlayer player)
        {
            if (bRPM.IsValid(bRPM.Read<ulong>(pClientPlayer + Offsets.PZ_ClientPlayer.m_pAttachedControllable)))
            {
                ulong num = bRPM.Read<ulong>(bRPM.Read<ulong>(pClientPlayer + Offsets.PZ_ClientPlayer.m_character)) - 8;
                if (bRPM.IsValid(num))
                {
                    player.InVehicle = true;
                    ulong num2 = bRPM.Read<ulong>(pClientPlayer + Offsets.PZ_ClientPlayer.m_pAttachedControllable);
                    if (bRPM.IsValid(num2))
                    {
                        ulong num3 = bRPM.Read<ulong>(num2 + Offsets.PZ_ClientSoldierEntity.m_data);
                        if (bRPM.IsValid(num3))
                        {
                            string text = bRPM.ReadString(bRPM.Read<UInt64>(num3 + Offsets.PZ_VehicleEntityData.m_NameSid), 0X14);
                            if (text.Length > 11)
                            {
                                ulong num4 = bRPM.Read<ulong>(bRPM.Read<UInt64>(num + Offsets.PZ_ClientPlayer.m_pAttachedControllable) + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);
                                player.VehicleHealth = bRPM.Read<float>(num4 + Offsets.PZ_HealthComponent.m_vehicleHealth);
                                player.VehicleMaxHealth = bRPM.Read<float>(num3 + Offsets.PZ_VehicleEntityData.m_FrontMaxHealth);
                                player.VehicleName = text.Remove(0, 11);
                                player.IsDriver = true;
                            }
                        }
                    }
                }
                return num;
            }
            return bRPM.Read<ulong>(pClientPlayer + Offsets.PZ_ClientPlayer.m_pControlledControllable);
        }
        private bool GetBoneById(UInt64 pEnemySoldier, uint Id, out Vector3 _World)
        {
            _World = new Vector3();

            UInt64 pRagdollComp = bRPM.Read<UInt64>(pEnemySoldier + Offsets.PZ_ClientSoldierEntity.m_ragdollComponent);


            UInt64 pQuatTransform = bRPM.Read<UInt64>(pRagdollComp + 0X18);


            _World = bRPM.Read<Vector3>(pQuatTransform + Id * 0x20);
            return true;
        }
        private ulong[] data = new ulong[70];
        private ulong pLocalPlayerDecrypted;

        private void MainScan()
        {
            Array.Clear(this.data, 0, this.data.Length);
            players.Clear();
            MemoryMappedViewStream memoryMappedViewStream = MemoryMappedFile.OpenExisting("DataSend").CreateViewStream();
            byte[] array = new byte[560];
            memoryMappedViewStream.Read(array, 0, 560);
            Buffer.BlockCopy(array, 0, this.data, 0, array.Length);
            memoryMappedViewStream.Flush();
            memoryMappedViewStream.Dispose();
            #region Get Local Player


            for (uint i = 0; i < 70; i++)
            {
                GPlayer player = new GPlayer();

                ulong num = this.data[i];
                if (bRPM.IsValid(num))
                {
                    string text = bRPM.ReadString2(num + Offsets.PZ_ClientPlayer.szName, 0xF);
                    string value = Pro.NamePlayer.Substring(0, 4);
                    if (text.Substring(0, 4).Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        pLocalPlayerDecrypted = num;

                        ulong pLocalSoldier = PegaClienteSoldadodEntidade(num, localPlayer);
                        localPlayer.pSoldier = pLocalSoldier;
                        localPlayer.Team = bRPM.Read<uint>(num + Offsets.PZ_ClientPlayer.m_teamId);

                        if (!bRPM.IsValid(pLocalSoldier))
                        {
                            return;
                        }

                        ulong pHealthComponent = bRPM.Read<ulong>(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);

                        // Health
                        localPlayer.Health = bRPM.Read<float>(pHealthComponent + Offsets.PZ_HealthComponent.m_Health);
                        localPlayer.MaxHealth = bRPM.Read<float>(pHealthComponent + Offsets.PZ_HealthComponent.m_MaxHealth);

                        // Origin
                        localPlayer.Origin = bRPM.Read<Vector3>(pLocalSoldier + 0X0990);
                        localPlayer.IsOccluded = bRPM.Read<byte>(pLocalSoldier + Offsets.PZ_ClientSoldierEntity.m_occluded);
                        localPlayer.IsFriendly = (player.Team == localPlayer.Team);

                        #endregion


                        #region Get Other Players by Id

                    }
                    ulong num9 = PegaClienteSoldadodEntidade(num, player);
                     if (bRPM.IsValid(num9) && num != pLocalPlayerDecrypted)
                      {
                    ulong pGameRenderer = bRPM.Read<ulong>(Offsets.OFFSET_GAMERENDERER);
                    ulong pRenderView = bRPM.Read<ulong>(pGameRenderer + Offsets.PZ_GameRenderer.m_pRenderView);
                    // Read Screen Matrix4x4
                    localPlayer.ViewProj = bRPM.Read<Matrix4x4>(pRenderView + Offsets.PZ_RenderView.m_viewProj);

                        if (player.IsSpectator)
                            spectatorCount++;

                     player.Name = bRPM.ReadString(num + Offsets.PZ_ClientPlayer.szName, 10);
                     player.pSoldier = num9;

                    ulong pEnemyHealthComponent = bRPM.Read<ulong>(num9 + Offsets.PZ_ClientSoldierEntity.m_pHealthComponent);
                        // Health
                        player.Health = bRPM.Read<float>(pEnemyHealthComponent + Offsets.PZ_HealthComponent.m_Health);
                        player.MaxHealth = bRPM.Read<float>(pEnemyHealthComponent + Offsets.PZ_HealthComponent.m_MaxHealth);


                        // Origin (Position in Game X, Y, Z)
                        player.Origin = bRPM.Read<Vector3>(num9 + 0X0990);
                        player.Team = bRPM.Read<uint>(num + Offsets.PZ_ClientPlayer.m_teamId);
                        player.Pose = bRPM.Read<uint>(num9 + Offsets.PZ_ClientSoldierEntity.m_poseType);
                        player.Yaw = bRPM.Read<float>(num9 + Offsets.PZ_ClientSoldierEntity.m_authorativeYaw);
                        player.IsOccluded = bRPM.Read<byte>(num9 + Offsets.PZ_ClientSoldierEntity.m_occluded);
                        player.IsFriendly = (player.Team == localPlayer.Team);
                        // Distance to You
                        player.Distance = Vector3.Distance(localPlayer.Origin, player.Origin);
                        bRANDER.SetViewProjection(localPlayer.ViewProj);
                        players.Add(player);

                        if (player.IsValid())
                        {
                            // Player Bone
                            bBoneOk = (GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_HEAD, out player.Bone.BONE_HEAD)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTELBOWROLL, out player.Bone.BONE_LEFTELBOWROLL)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTFOOT, out player.Bone.BONE_LEFTFOOT)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTHAND, out player.Bone.BONE_LEFTHAND)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTKNEEROLL, out player.Bone.BONE_LEFTKNEEROLL)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_LEFTSHOULDER, out player.Bone.BONE_LEFTSHOULDER)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_NECK, out player.Bone.BONE_NECK)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTELBOWROLL, out player.Bone.BONE_RIGHTELBOWROLL)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTFOOT, out player.Bone.BONE_RIGHTFOOT)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTHAND, out player.Bone.BONE_RIGHTHAND)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTKNEEROLL, out player.Bone.BONE_RIGHTKNEEROLL)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_RIGHTSHOULDER, out player.Bone.BONE_RIGHTSHOULDER)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE, out player.Bone.BONE_SPINE)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE1, out player.Bone.BONE_SPINE1)
                                    && GetBoneById(num9, (int)Offsets.PZ_UpdatePoseResultData.BONES.BONE_SPINE2, out player.Bone.BONE_SPINE2));



                            #region Drawing ESP on Overlay

                            // Desconsidera Aliados
                            if (!bEspAllies && (player.Team == localPlayer.Team))
                                continue;

                            #region ESP Bone
                            if (bBoneOk && ESP_Bone)
                                DrawBone(player);
                            #endregion

                            Vector3 textPos = new Vector3(player.Origin.X, player.Origin.Y, player.Origin.Z);
                            if (bRANDER.WorldToScreen(textPos, out Vector3 w2sFoot) && bRANDER.WorldToScreen(textPos, player.Pose, out Vector3 w2sHead))
                            {
                                float H = w2sFoot.Y - w2sHead.Y;
                                float W = H / 2;
                                float X = w2sHead.X - W / 2;

                                float heightoffset = Distance3D(w2sFoot, w2sHead);
                                #region ESP Color
                                var color = (player.Team == localPlayer.Team) ? Color.FromArgb(0, 0, 255) : player.IsVisible() ? Color.FromArgb(0, 255, 0) : Color.FromArgb(255, 0, 0);
                                var colordist = (player.Team == localPlayer.Team) ? Color.FromArgb(0, 0, 255) : player.IsVisible() ? Color.FromArgb(255, 0, 137) : Color.FromArgb(255, 156, 210);
                                var applycolor = (ESP_Spot ? player.Distance <= 50 ? colordist : color : color);

                                #endregion

                                #region ESP Box
                                // ESP Box
                                if (ESP_Box)
                                    if (bEsp3D)
                                        DrawAABB(player.GetAABB(), player.Origin, player.Yaw, applycolor); // 3D Box      
                                    else
                                    {
                                        float factor = (heightoffset / 5);

                                        Vector3 m2 = new Vector3(w2sHead.X - factor, w2sHead.Y, 0);
                                        Vector3 m1 = new Vector3(w2sHead.X + factor, w2sHead.Y, 0);
                                        Vector3 m3 = new Vector3(w2sFoot.X - factor, w2sFoot.Y, 0);
                                        Vector3 m4 = new Vector3(w2sFoot.X + factor, w2sFoot.Y, 0);

                                        bRANDER.DrawLine(m1.X, m1.Y, m2.X, m2.Y, 3, applycolor);
                                        bRANDER.DrawLine(m2.X, m2.Y, m3.X, m3.Y, 3, applycolor);
                                        bRANDER.DrawLine(m3.X, m3.Y, m4.X, m4.Y, 3, applycolor);
                                        bRANDER.DrawLine(m4.X, m4.Y, m1.X, m1.Y, 3, applycolor);
                                    }
                                #endregion

                                #region ESP Vehicle
                                if (ESP_Vehicle)
                                    DrawAABB(player.VehicleAABB, player.VehicleTranfsorm, applycolor);
                                #endregion

                                #region ESP Name
                                if (ESP_Name)
                                {
                                    int fontsize = 20;
                                    float offset = (player.Name.Length * fontsize) / 5;
                                    bRANDER.DrawString(w2sHead.X - offset, w2sHead.Y - (heightoffset / 4) - fontsize, player.Name, Color.FromArgb(255, 255, 255));
                                }
                                #endregion

                                #region ESP Distance
                                if (ESP_Distance)
                                {
                                    bRANDER.DrawString(w2sFoot.X, w2sFoot.Y, (int)player.Distance + "m", Color.FromArgb(240, 240, 240, 255));
                                }
                                #endregion

                                #region ESP Health
                                if (ESP_Health)
                                {
                                    if (player.Health <= 0)
                                        player.Health = 1;

                                    if (player.MaxHealth < player.Health)
                                        player.MaxHealth = 100;

                                    float factor = (-heightoffset / 4 - 8);
                                    Vector3 m1 = new Vector3(w2sHead.X + factor, w2sHead.Y, 0);
                                    Vector3 m2 = new Vector3(w2sFoot.X + factor, w2sFoot.Y, 0);

                                    int progress = (int)((float)player.Health / ((float)player.MaxHealth / 100));
                                    float perc = (player.Health / player.MaxHealth);
                                    int xnxx = (int)perc;
                                    float thicc = heightoffset / 18;
                                    if (thicc < 4) thicc = 4;
                                    var HealthColor = Color.FromArgb(1 - xnxx, xnxx, 0);

                                    bRANDER.DrawLine(m1.X + thicc / 2, m1.Y - 2, m2.X + thicc / 2, m2.Y + 2, thicc + 4, Color.FromArgb(1, 1, 1));
                                    bRANDER.DrawLine(m1.X + thicc / 2, m1.Y + ((m2.Y - m1.Y) * (1 - perc)), m2.X + thicc / 2, m2.Y, thicc, Color.Green);
                                    bRANDER.DrawString(m1.X + thicc - 9, m1.Y - 10, "%" + progress, Color.FromArgb(255, 255, 255, 255));

                                }
                                #endregion

                                #region ESP DMG Vehicle
                                if (ESP_dmg_Vehicle)
                                {
                                    if (player.InVehicle && player.IsDriver)
                                        DrawHealth((int)X, (int)w2sHead.Y - 30, (int)W + 65, 6, (int)player.VehicleHealth, (int)player.VehicleMaxHealth);
                                }
                                #endregion

                                if (ESP_Line)
                                {
                                    bRANDER.DrawLine(w2sFoot.X, w2sFoot.Y, (rect.Right - rect.Left) / 2, rect.Bottom - rect.Top, 3, applycolor);

                                }

                                if (ESP_Spot && player.Distance <= 50)
                                {
                                    DrawProximityAlert(rect.Width / 2 + 300, rect.Height - 80, 155, 50);
                                }

                                if (ESP_SpectatorWarn && spectatorCount > 0)
                                {
                                    DrawSpectatorWarn(rect.Bottom - 125, 25, 350, 55);
                                }
                            }
                            #endregion

                        }

                        #endregion
                   }
                }
            }
        }

        #endregion

        #region Keys Stuff
        public void KeyAssign()
        {
            KeysMgr keyMgr = new KeysMgr();
            keyMgr.AddKey(Keys.Home);     // MENU
            keyMgr.AddKey(Keys.Up);       // UP
            keyMgr.AddKey(Keys.Down);     // DOWN
            keyMgr.AddKey(Keys.Right);    // CHANGE OPTION
            keyMgr.AddKey(Keys.Delete);   // QUIT

            keyMgr.AddKey(Keys.F6);       // Clear Weapon Data Bank (Collection)

            keyMgr.AddKey(Keys.F9);       // ATALHO 1
            keyMgr.AddKey(Keys.F10);      // ATALHO 2
            keyMgr.AddKey(Keys.F11);      // ATALHO 3
            keyMgr.AddKey(Keys.F12);      // ATALHO 4

            keyMgr.AddKey(Keys.CapsLock);  // Aimbot Activate 1
            keyMgr.AddKey(Keys.RButton);   // Aimbot Activate 2

            keyMgr.AddKey(Keys.PageUp);    // Optimized Settings
            keyMgr.AddKey(Keys.PageDown);  // Default Settings

            keyMgr.KeyDownEvent += new KeysMgr.KeyHandler(KeyDownEvent);
        }

        public static bool IsKeyDown(int key)
        {
            return Convert.ToBoolean(NativeMethods.GetKeyState(key) & NativeMethods.KEY_PRESSED);
        }

        private void KeyDownEvent(int Id, string Name)
        {
            switch ((Keys)Id)
            {
                case Keys.Home:
                    this.bMenuControl = !this.bMenuControl;
                    break;
                case Keys.Right:
                    SelectMenuItem();
                    break;
                case Keys.Up:
                    CycleMenuUp();
                    break;
                case Keys.Down:
                    CycleMenuDown();
                    break;
            }

        }
        #endregion

        #region Menu Stuff

        private bool bMenuControl = true;
        private bool bEsp3D = false;
        private bool bEspAllies = false;
        private bool bCrosshair = true;

        private enum mnIndex
        {

            MN_ESP_NAME = 0,
            MN_ESP_BOX = 1,
            MN_ESP_3D = 2,
            MN_ESP_HEALTH = 3,
            MN_ESP_HEALTH_VEHICLE = 4,
            MN_ESP_BON = 5,
            MN_ESP_DISTANCE = 6,
            MN_ESP_VEHICLE = 7,
            MN_ESP_ALLIES = 8,
            MN_ESP_LINE = 9,
            MN_ESP_SPOT = 10,
            MN_CROSSHAIR = 11,
            MN_SPECTATORWARN = 12

        };
        private mnIndex currMnIndex = mnIndex.MN_ESP_NAME;
        private int LastMenuIndex = Enum.GetNames(typeof(mnIndex)).Length - 1;

        private enum mnEspMode
        {
            NONE,
            MINIMAL,
            PARTIAL,
            FULL
        };
        private mnEspMode currMnEspMode = mnEspMode.FULL;


        private void CycleMenuDown()
        {
            if (bMenuControl)
                currMnIndex = (mnIndex)((int)currMnIndex >= LastMenuIndex ? 0 : (int)currMnIndex + 1);
        }

        private void CycleMenuUp()
        {
            if (bMenuControl)
                currMnIndex = (mnIndex)((int)currMnIndex <= 0 ? LastMenuIndex : (int)currMnIndex - 1);
        }

        private void SelectMenuItem()
        {
            switch (currMnIndex)
            {

                case mnIndex.MN_ESP_NAME:
                    ESP_Name = !ESP_Name;
                    break;
                case mnIndex.MN_ESP_BOX:
                    ESP_Box = !ESP_Box;
                    break;
                case mnIndex.MN_ESP_3D:
                    bEsp3D = !bEsp3D;
                    break;
                case mnIndex.MN_ESP_HEALTH:
                    ESP_Health = !ESP_Health;
                    break;
                case mnIndex.MN_ESP_HEALTH_VEHICLE:
                    ESP_dmg_Vehicle = !ESP_dmg_Vehicle;
                    break;
                case mnIndex.MN_ESP_BON:
                    ESP_Bone = !ESP_Bone;
                    break;
                case mnIndex.MN_ESP_DISTANCE:
                    ESP_Distance = !ESP_Distance;
                    break;
                case mnIndex.MN_ESP_VEHICLE:
                    ESP_Vehicle = !ESP_Vehicle;
                    break;

                case mnIndex.MN_ESP_ALLIES:
                    bEspAllies = !bEspAllies;
                    break;

                case mnIndex.MN_ESP_LINE:
                    ESP_Line = !ESP_Line;
                    break;

                case mnIndex.MN_ESP_SPOT:
                    ESP_Spot = !ESP_Spot;
                    break;


                case mnIndex.MN_CROSSHAIR:
                    bCrosshair = !bCrosshair;
                    break;

                case mnIndex.MN_SPECTATORWARN:
                    ESP_SpectatorWarn = !ESP_SpectatorWarn;
                    break;
            }
        }

        private string GetMenuString(mnIndex idx)
        {
            string result = "";

            switch (idx)
            {

                case mnIndex.MN_ESP_NAME:
                    result = "ESP NAME : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Name) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_BOX:
                    result = "ESP BOX : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Box) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_3D:
                    result = "ESP 2D/3D : " + ((currMnEspMode == mnEspMode.NONE) ? "[ OFF ]" : (bEsp3D) ? "[ 3D ]" : "[ 2D ]");
                    break;
                case mnIndex.MN_ESP_HEALTH:
                    result = "ESP HEALTH : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Health) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_HEALTH_VEHICLE:
                    result = "ESP DMG VEHICLE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_dmg_Vehicle) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_BON:
                    result = "ESP SKELETON : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Bone) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_DISTANCE:
                    result = "ESP DISTANCE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Distance) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_VEHICLE:
                    result = "ESP VEHICLE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Vehicle) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_ALLIES:
                    result = "ESP FRIENDS : " + (((currMnEspMode != mnEspMode.NONE) && bEspAllies) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_LINE:
                    result = "ESP LINE : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Line) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_ESP_SPOT:
                    result = "ESP SPOT : " + (((currMnEspMode != mnEspMode.NONE) && ESP_Spot) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_CROSSHAIR:
                    result = "CROSSHAIR : " + (((currMnEspMode != mnEspMode.NONE) && bCrosshair) ? "[ ON ]" : "[ OFF ]");
                    break;
                case mnIndex.MN_SPECTATORWARN:
                    result = "ESP SPECTATOR : " + (((currMnEspMode != mnEspMode.NONE) && ESP_SpectatorWarn) ? "[ ON ]" : "[ OFF ]");
                    break;
            }

            return result;
        }

        #endregion



        #region Draw Stuff

        #region Draw - Variables



        // ESP OPTIONS
        private bool ESP_Box = true,
            ESP_Bone = false,
            ESP_Name = false,
            ESP_Health = true,
            ESP_Distance = false,
            ESP_Vehicle = true,
            ESP_Line = false,
            ESP_Spot = true,
            ESP_dmg_Vehicle = true,
        ESP_SpectatorWarn = true;

        #endregion

        #region Draw - Info

        public Vector3 Multiply(Vector3 vector, Matrix4x4 mat)
        {
            return new Vector3(mat.M11 * vector.X + mat.M21 * vector.Y + mat.M31 * vector.Z,
                               mat.M12 * vector.X + mat.M22 * vector.Y + mat.M32 * vector.Z,
                               mat.M13 * vector.X + mat.M23 * vector.Y + mat.M33 * vector.Z);
        }
        private void DrawAABB(AxisAlignedBox aabb, Matrix4x4 tranform, Color color)
        {
            Vector3 m_Position = new Vector3(tranform.M41, tranform.M42, tranform.M43);
            Vector3 fld = Multiply(new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 brt = Multiply(new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 bld = Multiply(new Vector3(aabb.Min.X, aabb.Min.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 frt = Multiply(new Vector3(aabb.Max.X, aabb.Max.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 frd = Multiply(new Vector3(aabb.Max.X, aabb.Min.Y, aabb.Min.Z), tranform) + m_Position;
            Vector3 brb = Multiply(new Vector3(aabb.Max.X, aabb.Min.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 blt = Multiply(new Vector3(aabb.Min.X, aabb.Max.Y, aabb.Max.Z), tranform) + m_Position;
            Vector3 flt = Multiply(new Vector3(aabb.Min.X, aabb.Max.Y, aabb.Min.Z), tranform) + m_Position;

            #region bRANDER.WorldToScreen
            if (!bRANDER.WorldToScreen(fld, out fld) || !bRANDER.WorldToScreen(brt, out brt)
                || !bRANDER.WorldToScreen(bld, out bld) || !bRANDER.WorldToScreen(frt, out frt)
                || !bRANDER.WorldToScreen(frd, out frd) || !bRANDER.WorldToScreen(brb, out brb)
                || !bRANDER.WorldToScreen(blt, out blt) || !bRANDER.WorldToScreen(flt, out flt))
                return;
            #endregion

            #region DrawLines
            bRANDER.DrawLine(fld.X, fld.Y, flt.X, flt.Y,1, color);
            bRANDER.DrawLine(flt.X, flt.Y, frt.X, frt.Y,1, color);
            bRANDER.DrawLine(frt.X, frt.Y, frd.X, frd.Y,1, color);
            bRANDER.DrawLine(frd.X, frd.Y, fld.X, fld.Y,1, color);
            bRANDER.DrawLine(bld.X, bld.Y, blt.X, blt.Y,1, color);
            bRANDER.DrawLine(blt.X, blt.Y, brt.X, brt.Y,1, color);
            bRANDER.DrawLine(brt.X, brt.Y, brb.X, brb.Y,1, color);
            bRANDER.DrawLine(brb.X, brb.Y, bld.X, bld.Y,1, color);
            bRANDER.DrawLine(fld.X, fld.Y, bld.X, bld.Y,1, color);
            bRANDER.DrawLine(frd.X, frd.Y, brb.X, brb.Y,1, color);
            bRANDER.DrawLine(flt.X, flt.Y, blt.X, blt.Y,1, color);
            bRANDER.DrawLine(frt.X, frt.Y, brt.X, brt.Y,1, color);
            #endregion
        }
        private void DrawAABB(AxisAlignedBox aabb, Vector3 m_Position, float Yaw, Color color)
        {
            float cosY = (float)Math.Cos(Yaw);
            float sinY = (float)Math.Sin(Yaw);

            Vector3 fld = new Vector3(aabb.Min.Z * cosY - aabb.Min.X * sinY, aabb.Min.Y, aabb.Min.X * cosY + aabb.Min.Z * sinY) + m_Position; // 0
            Vector3 brt = new Vector3(aabb.Min.Z * cosY - aabb.Max.X * sinY, aabb.Min.Y, aabb.Max.X * cosY + aabb.Min.Z * sinY) + m_Position; // 1
            Vector3 bld = new Vector3(aabb.Max.Z * cosY - aabb.Max.X * sinY, aabb.Min.Y, aabb.Max.X * cosY + aabb.Max.Z * sinY) + m_Position; // 2
            Vector3 frt = new Vector3(aabb.Max.Z * cosY - aabb.Min.X * sinY, aabb.Min.Y, aabb.Min.X * cosY + aabb.Max.Z * sinY) + m_Position; // 3
            Vector3 frd = new Vector3(aabb.Max.Z * cosY - aabb.Min.X * sinY, aabb.Max.Y, aabb.Min.X * cosY + aabb.Max.Z * sinY) + m_Position; // 4
            Vector3 brb = new Vector3(aabb.Min.Z * cosY - aabb.Min.X * sinY, aabb.Max.Y, aabb.Min.X * cosY + aabb.Min.Z * sinY) + m_Position; // 5
            Vector3 blt = new Vector3(aabb.Min.Z * cosY - aabb.Max.X * sinY, aabb.Max.Y, aabb.Max.X * cosY + aabb.Min.Z * sinY) + m_Position; // 6
            Vector3 flt = new Vector3(aabb.Max.Z * cosY - aabb.Max.X * sinY, aabb.Max.Y, aabb.Max.X * cosY + aabb.Max.Z * sinY) + m_Position; // 7

            #region bRANDER.WorldToScreen
            if (!bRANDER.WorldToScreen(fld, out fld) || !bRANDER.WorldToScreen(brt, out brt)
                || !bRANDER.WorldToScreen(bld, out bld) || !bRANDER.WorldToScreen(frt, out frt)
                || !bRANDER.WorldToScreen(frd, out frd) || !bRANDER.WorldToScreen(brb, out brb)
                || !bRANDER.WorldToScreen(blt, out blt) || !bRANDER.WorldToScreen(flt, out flt))
                return;
            #endregion

            #region DrawLines
            bRANDER.DrawLine(fld.X, fld.Y, brt.X, brt.Y,1, color);
            bRANDER.DrawLine(brb.X, brb.Y, blt.X, blt.Y,1, color);
            bRANDER.DrawLine(fld.X, fld.Y, brb.X, brb.Y,1, color);
            bRANDER.DrawLine(brt.X, brt.Y, blt.X, blt.Y,1, color);

            bRANDER.DrawLine(frt.X, frt.Y, bld.X, bld.Y,1, color);
            bRANDER.DrawLine(frd.X, frd.Y, flt.X, flt.Y,1, color);
            bRANDER.DrawLine(frt.X, frt.Y, frd.X, frd.Y,1, color);
            bRANDER.DrawLine(bld.X, bld.Y, flt.X, flt.Y,1, color);

            bRANDER.DrawLine(frt.X, frt.Y, fld.X, fld.Y,1, color);
            bRANDER.DrawLine(frd.X, frd.Y, brb.X, brb.Y,1, color);
            bRANDER.DrawLine(brt.X, brt.Y, bld.X, bld.Y,1, color);
            bRANDER.DrawLine(blt.X, blt.Y, flt.X, flt.Y,1, color);
            #endregion
        }


        private void DrawMenu(int x, int y)
        {
            bRANDER.DrawBox(x, y, rect.Width /4 - 280, rect.Height /2 - 220,2, Color.Black, true);

            foreach (mnIndex MnIdx in Enum.GetValues(typeof(mnIndex)))
            {
                var color = Color.Red;
                if (currMnIndex == MnIdx)
                    color = Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256));
                bRANDER.DrawString( x + 5, y = y + 20, GetMenuString(MnIdx), color);
            }
        }

        #endregion

        #region Draw - ESP

        private void DrawBone(GPlayer player)
        {
            Vector3 BONE_HEAD,
            BONE_NECK,
            BONE_SPINE2,
            BONE_SPINE1,
            BONE_SPINE,
            BONE_LEFTSHOULDER,
            BONE_RIGHTSHOULDER,
            BONE_LEFTELBOWROLL,
            BONE_RIGHTELBOWROLL,
            BONE_LEFTHAND,
            BONE_RIGHTHAND,
            BONE_LEFTKNEEROLL,
            BONE_RIGHTKNEEROLL,
            BONE_LEFTFOOT,
            BONE_RIGHTFOOT;

            if (bRANDER.WorldToScreen(player.Bone.BONE_HEAD, out BONE_HEAD) &&
           bRANDER.WorldToScreen(player.Bone.BONE_NECK, out BONE_NECK) &&
            bRANDER.WorldToScreen(player.Bone.BONE_SPINE2, out BONE_SPINE2) &&
            bRANDER.WorldToScreen(player.Bone.BONE_SPINE1, out BONE_SPINE1) &&
            bRANDER.WorldToScreen(player.Bone.BONE_SPINE, out BONE_SPINE) &&
            bRANDER.WorldToScreen(player.Bone.BONE_LEFTSHOULDER, out BONE_LEFTSHOULDER) &&
            bRANDER.WorldToScreen(player.Bone.BONE_RIGHTSHOULDER, out BONE_RIGHTSHOULDER) &&
            bRANDER.WorldToScreen(player.Bone.BONE_LEFTELBOWROLL, out BONE_LEFTELBOWROLL) &&
            bRANDER.WorldToScreen(player.Bone.BONE_RIGHTELBOWROLL, out BONE_RIGHTELBOWROLL) &&
            bRANDER.WorldToScreen(player.Bone.BONE_LEFTHAND, out BONE_LEFTHAND) &&
            bRANDER.WorldToScreen(player.Bone.BONE_RIGHTHAND, out BONE_RIGHTHAND) &&
            bRANDER.WorldToScreen(player.Bone.BONE_LEFTKNEEROLL, out BONE_LEFTKNEEROLL) &&
            bRANDER.WorldToScreen(player.Bone.BONE_RIGHTKNEEROLL, out BONE_RIGHTKNEEROLL) &&
            bRANDER.WorldToScreen(player.Bone.BONE_LEFTFOOT, out BONE_LEFTFOOT) &&
            bRANDER.WorldToScreen(player.Bone.BONE_RIGHTFOOT, out BONE_RIGHTFOOT))
            {
                int stroke = 3;
                int strokeW = stroke % 2 == 0 ? stroke / 2 : (stroke - 1) / 2;

                // Color
                var skeletonColor = (player.Team == localPlayer.Team) ? Color.FromArgb(0, 255, 231, 255) : player.IsVisible() ? Color.FromArgb(251, 255, 0, 255) : Color.FromArgb(255, 161, 0, 255);
                var colordist = (player.Team == localPlayer.Team) ? Color.FromArgb(0, 0, 255, 255) : player.IsVisible() ? Color.FromArgb(5, 142, 246, 255) : Color.FromArgb(151, 247, 241, 255);
                var applycolor = (ESP_Spot ? player.Distance <= 50 ? colordist : skeletonColor : skeletonColor);

                // RECT's
                bRANDER.DrawCircle((int)BONE_HEAD.X - strokeW, (int)BONE_HEAD.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_NECK.X - strokeW, (int)BONE_NECK.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_LEFTSHOULDER.X - strokeW, (int)BONE_LEFTSHOULDER.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_LEFTELBOWROLL.X - strokeW, (int)BONE_LEFTELBOWROLL.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_LEFTHAND.X - strokeW, (int)BONE_LEFTHAND.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_RIGHTSHOULDER.X - strokeW, (int)BONE_RIGHTSHOULDER.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_RIGHTELBOWROLL.X - strokeW, (int)BONE_RIGHTELBOWROLL.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_RIGHTHAND.X - strokeW, (int)BONE_RIGHTHAND.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_SPINE2.X - strokeW, (int)BONE_SPINE2.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_SPINE1.X - strokeW, (int)BONE_SPINE1.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_SPINE.X - strokeW, (int)BONE_SPINE.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_LEFTKNEEROLL.X - strokeW, (int)BONE_LEFTKNEEROLL.Y - strokeW, stroke, stroke, applycolor, true);
                bRANDER.DrawCircle((int)BONE_RIGHTKNEEROLL.X - strokeW, (int)BONE_RIGHTKNEEROLL.Y - strokeW, 2, 2, applycolor, true);
                bRANDER.DrawCircle((int)BONE_LEFTFOOT.X - strokeW, (int)BONE_LEFTFOOT.Y - strokeW, 2, 2, applycolor, true);
                bRANDER.DrawCircle((int)BONE_RIGHTFOOT.X - strokeW, (int)BONE_RIGHTFOOT.Y - strokeW, 2, 2, applycolor, true);

                // Head -> Neck
                bRANDER.DrawLine((int)BONE_HEAD.X, (int)BONE_HEAD.Y, (int)BONE_NECK.X, (int)BONE_NECK.Y, 1, applycolor);

                // Neck -> Left
                bRANDER.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_LEFTSHOULDER.X, (int)BONE_LEFTSHOULDER.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_LEFTSHOULDER.X, (int)BONE_LEFTSHOULDER.Y, (int)BONE_LEFTELBOWROLL.X, (int)BONE_LEFTELBOWROLL.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_LEFTELBOWROLL.X, (int)BONE_LEFTELBOWROLL.Y, (int)BONE_LEFTHAND.X, (int)BONE_LEFTHAND.Y, 1, applycolor);

                // Neck -> Right
                bRANDER.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_RIGHTSHOULDER.X, (int)BONE_RIGHTSHOULDER.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_RIGHTSHOULDER.X, (int)BONE_RIGHTSHOULDER.Y, (int)BONE_RIGHTELBOWROLL.X, (int)BONE_RIGHTELBOWROLL.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_RIGHTELBOWROLL.X, (int)BONE_RIGHTELBOWROLL.Y, (int)BONE_RIGHTHAND.X, (int)BONE_RIGHTHAND.Y, 1, applycolor);

                // Neck -> Center
                bRANDER.DrawLine((int)BONE_NECK.X, (int)BONE_NECK.Y, (int)BONE_SPINE2.X, (int)BONE_SPINE2.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_SPINE2.X, (int)BONE_SPINE2.Y, (int)BONE_SPINE1.X, (int)BONE_SPINE1.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_SPINE1.X, (int)BONE_SPINE1.Y, (int)BONE_SPINE.X, (int)BONE_SPINE.Y, 1, applycolor);

                // Spine -> Left
                bRANDER.DrawLine((int)BONE_SPINE.X, (int)BONE_SPINE.Y, (int)BONE_LEFTKNEEROLL.X, (int)BONE_LEFTKNEEROLL.Y, 1, applycolor);
                bRANDER.DrawLine((int)BONE_LEFTKNEEROLL.X, (int)BONE_LEFTKNEEROLL.Y, (int)BONE_LEFTFOOT.X, (int)BONE_LEFTFOOT.Y, 1, applycolor);

                // Spine -> Right
                bRANDER.DrawLine((int)BONE_SPINE.X, (int)BONE_SPINE.Y, (int)BONE_RIGHTKNEEROLL.X, (int)BONE_RIGHTKNEEROLL.Y, 1, applycolor);
               bRANDER.DrawLine((int)BONE_RIGHTKNEEROLL.X, (int)BONE_RIGHTKNEEROLL.Y, (int)BONE_RIGHTFOOT.X, (int)BONE_RIGHTFOOT.Y,1, applycolor);
            }
        }

        #endregion

        #endregion



        #region Utilities Stuff

        private void DrawHealth(int X, int Y, int W, int H, int Health, int MaxHealth)
        {
            if (Health <= 0)
                Health = 1;

            if (MaxHealth < Health)
                MaxHealth = 100;

            int progress = (int)((float)Health / ((float)MaxHealth / 100));
            int w = (int)((float)W / 100 * progress);

            if (w <= 2)
                w = 3;

            var color = Color.FromArgb(255, 0, 0, 255);
            if (progress >= 20) color = Color.FromArgb(255, 165, 0, 255);
            if (progress >= 40) color = Color.FromArgb(255, 255, 0, 255);
            if (progress >= 60) color = Color.FromArgb(173, 255, 47, 255);
            if (progress >= 80) color = Color.FromArgb(0, 255, 0, 255);

            bRANDER.DrawCircle(X, Y - 1, W + 1, H + 2, Color.FromArgb(1, 1, 1, 255),true);
            bRANDER.DrawCircle(X + 1, Y, w - 1, H, color, true);
            bRANDER.DrawString( X - 20, Y - 2, "%" + progress, Color.FromArgb(255, 255, 255, 255));
        }
        float Distance3D(Vector3 v1, Vector3 v2)
        {
            float x_d = (v2.X - v1.X);
            float y_d = (v2.Y - v1.Y);
            float z_d = (v2.Z - v1.Z);
            return (float)Math.Sqrt((x_d * x_d) + (y_d * y_d) + (z_d * z_d));
        }



        private void DrawProximityAlert(int X, int Y, int W, int H)
        {
            int fontsize = 16;
            bRANDER.DrawBox( X, Y, W, H,2, Color.Black, true);

            bRANDER.DrawString(X + 12 - (int)(fontsize / 2), Y + 15, "<< ENEMY CLOSE >>", Color.FromArgb(255, 0, 0, 255));
        }

        private void DrawSpectatorWarn(int X, int Y, int W, int H)
        {

            bRANDER.DrawBox(X, Y, W, H, 2, Color.Black, true);

            bRANDER.DrawString(X + 20, Y + 5, "<< SPECTATOR HERE. PLAY NORMAL >>", Color.FromArgb(255, 0, 0, 255));
        }
        #endregion

    }
}