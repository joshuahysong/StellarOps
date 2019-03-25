﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StellarOps.Contracts;
using StellarOps.Projectiles;
using StellarOps.Weapons;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StellarOps.Ships
{
    public abstract class ShipBase : Entity, IContainer, IFocusable
    {
        public Vector2 WorldPosition { get; set; }
        public List<Tile> TileMap { get; set; }
        public Vector2 Size { get; set; }
        public Vector2 Center => Size == null ? Vector2.Zero : new Vector2(Size.X / 2, Size.Y / 2);
        public List<IPawn> Pawns { get; set; }

        protected int[,] TileMapArtData;
        protected int[,] TileMapCollisionData;
        protected int[,] TileMapHealthData;
        protected float Thrust;
        protected float ManeuveringThrust;
        protected float MaxTurnRate;
        protected float MaxVelocity;

        private Vector2 _acceleration;
        private float _currentTurnRate;

        private WeaponBase _weapon;

        public ShipBase()
        {
            Pawns = new List<IPawn>();
            _weapon = new WeaponBase();
        }

        public override void Update(GameTime gameTime, Matrix parentTransform)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            HandleInput(deltaTime);

            // Continue rotation until turn rate reaches zero to simulate slowing
            if (_currentTurnRate > 0)
            {
                RotateClockwise(deltaTime);
            }
            else if (_currentTurnRate < 0)
            {
                RotateCounterClockwise(deltaTime);
            }

            Velocity += _acceleration * deltaTime;

            // Cap velocity to max velocity
            if (Velocity.LengthSquared() > MaxVelocity * MaxVelocity)
            {
                Velocity.Normalize();
                Velocity *= MaxVelocity;
            }
            _acceleration.X = 0;
            _acceleration.Y = 0;
            Position += Velocity * deltaTime;
            WorldPosition = Position;

            Pawns.ForEach(p => p.Update(gameTime, LocalTransform));

            if (MainGame.IsDebugging)
            {
                MainGame.Instance.ShipDebugEntries["Position"] = $"{Math.Round(Position.X)}, {Math.Round(Position.Y)}";
                MainGame.Instance.ShipDebugEntries["Velocity"] = $"{Math.Round(Velocity.X)}, {Math.Round(Velocity.Y)}";
                MainGame.Instance.ShipDebugEntries["Heading"] = $"{Math.Round(Heading, 2)}";
                MainGame.Instance.ShipDebugEntries["Velocity Heading"] = $"{Math.Round(Velocity.ToAngle(), 2)}";
                MainGame.Instance.ShipDebugEntries["World Tile"] = $"{Math.Floor(Position.X / MainGame.WorldTileSize)}, {Math.Floor(Position.Y / MainGame.WorldTileSize)}";
                MainGame.Instance.ShipDebugEntries["Current Turn Rate"] = $"{Math.Round(_currentTurnRate, 2)}";
            }
        }

        public void HandleInput(float deltaTime)
        {
            if (MainGame.Camera.Focus == this)
            {
                // Apply thrust
                if (Input.IsKeyPressed(Keys.W) || Input.IsKeyPressed(Keys.Up))
                {
                    _acceleration.X += Thrust * (float)Math.Cos(Heading);
                    _acceleration.Y += Thrust * (float)Math.Sin(Heading);
                }
                // Rotate Counter-Clockwise
                if (Input.IsKeyPressed(Keys.A) || Input.IsKeyPressed(Keys.Left))
                {
                    _currentTurnRate = _currentTurnRate - ManeuveringThrust < -MaxTurnRate ? -MaxTurnRate
                        : _currentTurnRate - ManeuveringThrust;
                }
                // Rotate Clockwise
                else if (Input.IsKeyPressed(Keys.D) || Input.IsKeyPressed(Keys.Right))
                {
                    _currentTurnRate = _currentTurnRate + ManeuveringThrust > MaxTurnRate ? MaxTurnRate
                        : _currentTurnRate + ManeuveringThrust;
                }
                // Rotate to face retro thurst heading
                else if (Input.IsKeyPressed(Keys.S) || Input.IsKeyPressed(Keys.Down))
                {
                    RotateToRetro(deltaTime, false);
                }
                // Rotate to face retro thurst heading and thrust to brake
                else if (Input.IsKeyPressed(Keys.X))
                {
                    RotateToRetro(deltaTime, true);
                }
                else
                {
                    if (_currentTurnRate != 0)
                    {
                        SlowDownManueveringThrust();
                    }
                }
                // Toggle to Player pawn control
                if (Input.IsKeyToggled(Keys.F) && !Input.ManagedKeys.Contains(Keys.F))
                {
                    Input.ManagedKeys.Add(Keys.F);
                    MainGame.Camera.Focus = MainGame.Player;
                    if (MainGame.Camera.Scale < 2f)
                    {
                        MainGame.Camera.Scale = 2F;
                    }
                }
                if (Input.IsKeyPressed(Keys.Space))
                {
                    _weapon.Fire(Heading, Velocity, Position);
                }

                // Tile click
                // TODO TEMP FOR DAMAGE TESTING
                if (Input.WasLeftMouseButtonClicked())
                {
                    Maybe<Tile> targetTile = GetTileByWorldPosition(Input.WorldMousePosition);
                    if (targetTile.HasValue)
                    {
                        if (targetTile.Value.Health > 0)
                        {
                            targetTile.Value.Health = targetTile.Value.Health - 25 < 0 ? 0 : targetTile.Value.Health - 25;
                            if (targetTile.Value.Health == 0)
                            {
                                targetTile.Value.TileType = TileType.Empty;
                                targetTile.Value.CollisionType = CollisionType.Open;
                            }
                        }
                    }
                }
            }
            else
            {
                if (_currentTurnRate != 0)
                {
                    SlowDownManueveringThrust();
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, Matrix parentTransform)
        {
            // Calculate global transform
            Matrix globalTransform = LocalTransform * parentTransform;

            // Get values from GlobalTransform for SpriteBatch and render sprite
            DecomposeMatrix(ref globalTransform, out Vector2 position, out float rotation, out Vector2 scale);

            // Tiles
            Vector2 origin = Center;
            TileMap.Where(t => t.TileType != TileType.Empty).ToList().ForEach(tile =>
            {
                Vector2 offset = new Vector2(tile.Location.X * MainGame.TileSize, tile.Location.Y * MainGame.TileSize);
                origin = Center - offset;
                Tuple<Texture2D, Rectangle> tileArtData = GetTileImage(tile);
                spriteBatch.Draw(tileArtData.Item1, Position, tileArtData.Item2, Color.White, Heading, origin / MainGame.TileScale, scale * MainGame.TileScale, SpriteEffects.None, 0.0f);
                if (tile.Health < 100 && tile. Health >= 75)
                {
                    spriteBatch.Draw(Art.Damage25, Position, null, Color.White, Heading, origin / MainGame.TileScale, scale * MainGame.TileScale, SpriteEffects.None, 0.0f);
                }
                if (tile.Health < 75 && tile.Health >= 50)
                {
                    spriteBatch.Draw(Art.Damage50, Position, null, Color.White, Heading, origin / MainGame.TileScale, scale * MainGame.TileScale, SpriteEffects.None, 0.0f);
                }
                if (tile.Health < 50)
                {
                    spriteBatch.Draw(Art.Damage75, Position, null, Color.White, Heading, origin / MainGame.TileScale, scale * MainGame.TileScale, SpriteEffects.None, 0.0f);
                }
            });

            Pawns.ForEach(p => p.Draw(spriteBatch, globalTransform));
        }

        private void RotateClockwise(float deltaTime)
        {
            Heading += _currentTurnRate * deltaTime;
            if (Heading > Math.PI)
            {
                Heading = (float)-Math.PI;
            }
        }

        private void RotateCounterClockwise(float deltaTime)
        {
            Heading += _currentTurnRate * deltaTime;
            if (Heading < -Math.PI)
            {
                Heading = (float)Math.PI;
            }
        }

        private void RotateToRetro(float deltaTime, bool IsBraking)
        {
            float movementHeading = Velocity.ToAngle();
            float retroHeading = movementHeading < 0 ? movementHeading + (float)Math.PI : movementHeading - (float)Math.PI;
            if (Heading != retroHeading  && !IsWithinBrakingRange())
            {
                double retroDegrees = (retroHeading + Math.PI) * (180.0 / Math.PI);
                double headingDegrees = (Heading + Math.PI) * (180.0 / Math.PI);
                double turnRateDegrees = Math.PI * 2 * (_currentTurnRate * deltaTime) / 100 * 360 * 2;
                turnRateDegrees = turnRateDegrees < 0 ? turnRateDegrees * -1 : turnRateDegrees;
                double retroOffset = headingDegrees < retroDegrees ? (headingDegrees + 360) - retroDegrees : headingDegrees - retroDegrees;

                double thrustMagnitude = Math.Round(_currentTurnRate / ManeuveringThrust * _currentTurnRate);
                thrustMagnitude = thrustMagnitude < 0 ? thrustMagnitude * -1 : thrustMagnitude;

                if (retroOffset >= 360 - turnRateDegrees || retroOffset <= turnRateDegrees)
                {
                    Heading = retroHeading;
                    _currentTurnRate = 0;
                }
                else if (retroOffset > thrustMagnitude && 360 - retroOffset > thrustMagnitude)
                {
                    if (retroOffset < 180)
                    {
                        _currentTurnRate = _currentTurnRate - ManeuveringThrust < -MaxTurnRate ? -MaxTurnRate
                            : _currentTurnRate - ManeuveringThrust;
                    }
                    else
                    {
                        _currentTurnRate = _currentTurnRate + ManeuveringThrust > MaxTurnRate ? MaxTurnRate
                            : _currentTurnRate + ManeuveringThrust;
                    }
                }
                else
                {
                    SlowDownManueveringThrust();
                }
            }
            else if (IsBraking)
            {
                if (IsWithinBrakingRange())
                {
                    Velocity = Vector2.Zero;
                }
                else if (Heading == retroHeading)
                {
                    _acceleration.X += Thrust * (float)Math.Cos(Heading);
                    _acceleration.Y += Thrust * (float)Math.Sin(Heading);
                }
            }
        }

        private bool IsWithinBrakingRange()
        {
            double brakingRange = MaxVelocity / 100;
            return Velocity.X < brakingRange && Velocity.X > -brakingRange && Velocity.Y < brakingRange && Velocity.Y > -brakingRange;
        }

        public abstract void UseTile(Vector2 position);

        public abstract string GetUsePrompt(Vector2 Position);

        public Maybe<Tile> GetTileByWorldPosition(Vector2 position)
        {
            return GetTileByRelativePosition(Vector2.Transform(position, Matrix.Invert(LocalTransform)));
        }

        /// <summary>
        /// Gets the tile object at the given position
        /// </summary>
        /// <param name="position">Relative Position to check</param>
        /// <returns>Tile at position</returns>
        public Maybe<Tile> GetTileByRelativePosition(Vector2 position)
        {
            Vector2 relativePosition = position + Center;
            return TileMap.Any(t => t.Bounds.Contains(position))
                ? Maybe<Tile>.Some(TileMap.First(t => t.Bounds.Contains(position)))
                : Maybe<Tile>.None;
        }

        public Maybe<Tile> GetTileByPoint(Point location)
        {
            return TileMap.Any(t => t.Location == location)
                ? Maybe<Tile>.Some(TileMap.First(t => t.Location == location))
                : Maybe<Tile>.None;
        }

        protected void SwitchControlToShip()
        {
            MainGame.Camera.Focus = this;
            if (MainGame.Camera.Scale > 1f)
            {
                MainGame.Camera.Scale = 1F;
            }
        }

        protected List<Tile> GetTileMap()
        {
            List<Tile> tileMap = new List<Tile>();
            int? rows = TileMapArtData.GetLength(0);
            int? columns = TileMapArtData.GetLength(1);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    Tile tile = new Tile
                    {
                        Location = new Point(x, y),
                        CollisionType = (CollisionType)TileMapCollisionData[y, x],
                        TileType = (TileType)TileMapArtData[y, x],
                        Health = TileMapHealthData[y, x],
                        North = y == 0 ? null : columns * y - columns + x,
                        NorthEast = y == 0 || x == columns - 1 ? null : columns * y - columns + x + 1,
                        East = x == columns - 1 ? null : (int?)tileMap.Count() + 1,
                        SouthEast = y == rows - 1 || x == columns - 1 ? null : columns * y + columns + x + 1,
                        South = y == rows - 1 ? null : columns * y + columns + x,
                        SouthWest = y == rows - 1 || x == 0 ? null : columns * y + columns + x - 1,
                        West = x == 0 ? null : (int?)tileMap.Count() - 1,
                        NorthWest = y == 0 || x == 0 ? null : columns * y - columns + x - 1,
                    };
                    Vector2 relativePosition = new Vector2(x * MainGame.TileSize, y * MainGame.TileSize);
                    relativePosition -= Center;
                    tile.Bounds = new Rectangle((int)relativePosition.X, (int)relativePosition.Y, MainGame.TileSize, MainGame.TileSize);
                    tileMap.Add(tile);
                }
            }

            return tileMap;
        }


        private void SlowDownManueveringThrust()
        {
            if (_currentTurnRate < 0)
            {
                _currentTurnRate = _currentTurnRate + ManeuveringThrust > 0 ? 0 : _currentTurnRate + ManeuveringThrust;
            }
            else if (_currentTurnRate > 0)
            {
                _currentTurnRate = _currentTurnRate - ManeuveringThrust < 0 ? 0 : _currentTurnRate - ManeuveringThrust;
            }
        }

        private Tuple<Texture2D, Rectangle> GetTileImage(Tile tile)
        {
            if (tile.TileType == TileType.Floor)
            {
                return new Tuple<Texture2D, Rectangle>(Art.Floor, new Rectangle(0,0, Art.TileSize, Art.TileSize));
            }
            if (tile.TileType == TileType.FlightConsole)
            {
                return new Tuple<Texture2D, Rectangle>(Art.FlightConsole, new Rectangle(0, 0, Art.TileSize, Art.TileSize));
            }
            if (tile.TileType == TileType.Hull)
            {
                bool north = tile.North == null ? false : TileMap[(int)tile.North].TileType == TileType.Hull;
                bool east = tile.East == null ? false : TileMap[(int)tile.East].TileType == TileType.Hull;
                bool south = tile.South == null ? false : TileMap[(int)tile.South].TileType == TileType.Hull;
                bool west = tile.West == null ? false : TileMap[(int)tile.West].TileType == TileType.Hull;
                //if (north && east && south && west)
                //{
                //    return Art.HullFull;
                //}
                if (north && east && !south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(3 * Art.TileSize, 1 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && east && south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(2 * Art.TileSize, 1 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && !east && south && west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(0 * Art.TileSize, 2 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (north && !east && !south && west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(1 * Art.TileSize, 2 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (north && !east && south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(1 * Art.TileSize, 0 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && east && !south && west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(0 * Art.TileSize, 0 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (north && !east && !south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(0 * Art.TileSize, 1 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && east && !south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(2 * Art.TileSize, 0 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && !east && south && !west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(1 * Art.TileSize, 1 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
                if (!north && !east && !south && west)
                {
                    return new Tuple<Texture2D, Rectangle>(Art.Hull, new Rectangle(3 * Art.TileSize, 0 * Art.TileSize, Art.TileSize, Art.TileSize));
                }
            }
            return new Tuple<Texture2D, Rectangle>(Art.FlightConsole, new Rectangle(0, 0, 0, 0));
        }
    }
}
