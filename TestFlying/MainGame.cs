﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TestFlying
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class MainGame : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D playerShip;
        Texture2D background;

        Vector2 centerScreen;
        Vector2 position;
        Vector2 velocity;
        Vector2 acceleration;
        float heading;
        const float thrust = 100.0f;

        Camera camera;

        public MainGame()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                IsFullScreen = false,
                PreferredBackBufferHeight = 900,
                PreferredBackBufferWidth = 1440
            };
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            position = new Vector2(GraphicsDevice.Viewport.Bounds.Width / 2, GraphicsDevice.Viewport.Bounds.Height / 2);
            centerScreen = position;
            camera = new Camera(this);
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            playerShip = Content.Load<Texture2D>("ship");
            background = Content.Load<Texture2D>("starfield");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            HandleInput(Keyboard.GetState(), gameTime);

            //if (position.X <= 0)
            //{
            //    position.X = GraphicsDevice.Viewport.Bounds.Width;
            //}
            //if (position.X > GraphicsDevice.Viewport.Bounds.Width)
            //{
            //    position.X = 0;
            //}
            //if (position.Y <= 0)
            //{
            //    position.Y = GraphicsDevice.Viewport.Bounds.Height;
            //}
            //if (position.Y > GraphicsDevice.Viewport.Bounds.Height)
            //{
            //    position.Y = 0;
            //}
            
            // Seconds passed since iteration of update
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            position += velocity * deltaTime;
            velocity += acceleration * deltaTime;
            acceleration.X = 0;
            acceleration.Y = 0;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            var shipCenter = new Vector2(playerShip.Width / 2, playerShip.Height / 2);
            var backgroundCenter = new Vector2(background.Width / 2, background.Height / 2);

            spriteBatch.Begin(SpriteSortMode.FrontToBack/*, null, null, null, null, null, camera.Transform*/);
            spriteBatch.Draw(background, position, null, Color.White, 0, backgroundCenter, 0.5f, SpriteEffects.None, 1f);
            //spriteBatch.Draw(background, new Rectangle(0, 0, 1440, 900), Color.White);
            spriteBatch.Draw(playerShip, centerScreen, null, Color.White, heading + (90 * (float)Math.PI / 180), shipCenter, 0.5f, SpriteEffects.None, 1f);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void HandleInput(KeyboardState KeyState, GameTime gameTime)
        {
            if (KeyState.IsKeyDown(Keys.W))
            {
                acceleration.X += thrust * (float)Math.Cos(heading + Math.PI);
                acceleration.Y += thrust * (float)Math.Sin(heading + Math.PI);
            }
            if (KeyState.IsKeyDown(Keys.A))
            {
                heading -= 0.05F;
            }
            if (KeyState.IsKeyDown(Keys.D))
            {
                heading += 0.05F;
            }
        }
    }
}
