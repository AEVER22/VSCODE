using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace GoatOptimizations
{
    public class PerformanceMod : Mod
    {
        private const float DustReductionFactor = 0.25f; // Reduction factor for the max dust count

        private bool worldLoaded; // Flag to track if the world has been loaded

        private int npcLoadDelay; // Delay counter for NPC loading
        private int projectileLoadDelay; // Delay counter for projectile loading
        private int dustLoadDelay; // Delay counter for dust loading
        private int itemLoadDelay; // Delay counter for item loading

        private int flareParticleUpdateRate = 5; // Update rate for flare particles

        private Rectangle screenBounds; // Rectangle for screen bounds

        public override void PostSetupContent()
        {
            PlayerPerformanceModPlayer modPlayer = ModContent.GetInstance<PlayerPerformanceModPlayer>();
            modPlayer.OnPostUpdate += PostUpdate;
        }

        private void PostUpdate(PlayerPerformanceModPlayer player)
        {
            if (Main.gamePaused || Main.instance.IsActive)
                return;

            int maxDustCount = (int)(Main.maxDust * DustReductionFactor); // Reduced max dust count

            screenBounds = new Rectangle((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);

            // Only process world loading optimizations if the world has not been loaded yet
            if (!worldLoaded)
            {
                OptimizeWorldLoading();
                worldLoaded = true;
            }

            // Process NPC loading with delay
            if (npcLoadDelay >= 0)
            {
                npcLoadDelay--;
            }
            else
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.active && screenBounds.Intersects(npc.getRect()))
                    {
                        // Render NPC
                        npc.AI();
                    }
                }
                npcLoadDelay = 10; // Delay value to distribute the load
            }

            // Process projectile loading with delay
            if (projectileLoadDelay >= 0)
            {
                projectileLoadDelay--;
            }
            else
            {
                int projectilesProcessed = 0;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && screenBounds.Intersects(new Rectangle((int)projectile.position.X, (int)projectile.position.Y, projectile.width, projectile.height)))
                    {
                        // Render projectile
                        projectile.AI();

                        projectilesProcessed++;
                        if (projectilesProcessed >= 5) // Process a limited number of projectiles per frame
                            break;
                    }
                }
                projectileLoadDelay = 20; // Delay value to distribute the load
            }

            // Process dust loading with delay
            if (dustLoadDelay >= 0)
            {
                dustLoadDelay--;
            }
            else
            {
                int dustCount = 0;
                for (int i = 0; i < Main.maxDust; i++)
                {
                    Dust dust = Main.dust[i];
                    if (dust.active)
                    {
                        if (screenBounds.Intersects(new Rectangle((int)dust.position.X, (int)dust.position.Y, 8, 8)))
                        {
                            // Increment the dust count
                            dustCount++;

                            // Check if we exceeded the max dust count
                            if (dustCount > maxDustCount)
                            {
                                // Skip rendering additional dust particles
                                dust.active = false;
                            }
                        }
                        else
                        {
                            // Unrender dust particle outside screen bounds
                            dust.active = false;
                        }
                    }
                }
                dustLoadDelay = 5; // Delay value to distribute the load
            }

            // Process item loading with delay
            if (itemLoadDelay >= 0)
            {
                itemLoadDelay--;
            }
            else
            {
                for (int i = 0; i < Main.maxItems; i++)
                {
                    Item item = Main.item[i];
                    if (item.active && screenBounds.Intersects(new Rectangle((int)item.position.X, (int)item.position.Y, item.width, item.height)))
                    {
                        // Render item
                        item.UpdateItem(60); // Assuming 60 frames per second
                    }
                }
                itemLoadDelay = 15; // Delay value to distribute the load
            }

            // Update flare particles at a reduced rate to improve performance in caves
            if (player.InCave)
            {
                if (flareParticleUpdateRate >= 0)
                {
                    flareParticleUpdateRate--;
                }
                else
                {
                    Dust.UpdateDust();
                    flareParticleUpdateRate = 5; // Update rate for flare particles in caves
                }
            }

            // Cap the attack speed to 40%
            foreach (var item in player.Player.inventory)
            {
                if (item.type > 0 && item.stack > 0 && item.useTime > 0)
                {
                    float maxUseTime = item.useTime / 2.5f; // Calculate the max useTime (40% of the original value)
                    item.useTime = (int)MathHelper.Clamp(item.useTime, maxUseTime, float.MaxValue);
                }
            }
        }

        private void OptimizeWorldLoading()
        {
            // Perform world loading optimizations here
            // Reduce unnecessary computations, implement loading delays, optimize data loading, etc.
        }
    }

    public class PlayerPerformanceModPlayer : ModPlayer
    {
        public event System.Action<PlayerPerformanceModPlayer> OnPostUpdate;

        private Player player; // Reference to the player object

        public override void Initialize()
        {
            player = Main.LocalPlayer;
        }

        public override void PostUpdate()
        {
            base.PostUpdate();
            OnPostUpdate?.Invoke(this);
        }

        public bool InCave
        {
            get { return player.ZoneRockLayerHeight || player.ZoneDirtLayerHeight || player.ZoneUnderworldHeight; }
        }
    }
}
