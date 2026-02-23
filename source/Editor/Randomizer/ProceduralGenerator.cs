using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using Snowberry.Editor.Tools;

namespace Snowberry.Editor.Randomizer;

/// <summary>
/// Procedural content generator for Celeste maps.
/// Generates rooms with platforming challenges, hazards, collectables, and connections.
/// </summary>
public class ProceduralGenerator {

    public enum Difficulty {
        Easy,
        Medium,
        Hard,
        Expert,
        Grandmaster
    }

    public enum RoomShape {
        Horizontal,
        Vertical,
        LShape,
        Square,
        TallShaft,
        WideHall
    }

    public enum ThemePreset {
        Forsaken,    // Chapter 1 style
        OldSite,     // Chapter 2 style
        Resort,      // Chapter 3 style
        Ridge,       // Chapter 4 style
        Temple,      // Chapter 5 style
        Reflection,  // Chapter 6 style
        Summit,      // Chapter 7 style
        Core,        // Chapter 8 style
        Random       // Mix of themes
    }

    public class GeneratorConfig {
        public int RoomCount { get; set; } = 5;
        public Difficulty Difficulty { get; set; } = Difficulty.Medium;
        public ThemePreset Theme { get; set; } = ThemePreset.Forsaken;
        public int Seed { get; set; } = 0;
        public bool LinearPath { get; set; } = true;
        public bool IncludeStrawberries { get; set; } = true;
        public bool IncludeSpinners { get; set; } = true;
        public bool IncludeSpikes { get; set; } = true;
        public bool IncludeSprings { get; set; } = true;
        public bool IncludeDashBlocks { get; set; } = false;
        public bool IncludeCrumbleBlocks { get; set; } = true;
        public bool IncludeMovingPlatforms { get; set; } = false;
        public float HazardDensity { get; set; } = 0.3f;
        public float PlatformDensity { get; set; } = 0.5f;
        public string MapName { get; set; } = "procedural_map";
        public int SideHeight { get; set; } = 23;
    }

    private Random rng;
    private GeneratorConfig config;
    private Map map;

    // Tileset character for solid ground (default Celeste dirt/stone)
    private char fgTileChar = '1';
    private char bgTileChar = '2';

    public ProceduralGenerator(GeneratorConfig config) {
        this.config = config;
        rng = config.Seed == 0 ? new Random() : new Random(config.Seed);
    }

    /// <summary>
    /// Generate a complete map with procedurally generated rooms.
    /// </summary>
    public Map Generate() {
        map = new Map(config.MapName);
        SelectThemeTiles();

        List<RoomBlueprint> blueprints = GenerateBlueprints();
        LayoutRooms(blueprints);

        for (int i = 0; i < blueprints.Count; i++) {
            var bp = blueprints[i];
            Room room = CreateRoom(bp, i);
            map.Rooms.Add(room);

            PopulateRoom(room, bp, i);
        }

        // Connect rooms with transitions
        ConnectRooms(blueprints);

        // Initialize all entities
        foreach (var room in map.Rooms)
            room.AllEntities.ForEach(e => e.InitializeAfter());

        Snowberry.LogInfo($"Procedural generation complete: {map.Rooms.Count} rooms, seed={config.Seed}");
        return map;
    }

    private void SelectThemeTiles() {
        fgTileChar = config.Theme switch {
            ThemePreset.Forsaken => '1',
            ThemePreset.OldSite => '3',
            ThemePreset.Resort => '4',
            ThemePreset.Ridge => '5',
            ThemePreset.Temple => '6',
            ThemePreset.Reflection => '7',
            ThemePreset.Summit => '8',
            ThemePreset.Core => '9',
            ThemePreset.Random => (char)('1' + rng.Next(9)),
            _ => '1'
        };
        bgTileChar = fgTileChar == '1' ? '2' : fgTileChar;
    }

    #region Blueprint Generation

    private List<RoomBlueprint> GenerateBlueprints() {
        var blueprints = new List<RoomBlueprint>();

        for (int i = 0; i < config.RoomCount; i++) {
            var shape = PickRoomShape(i);
            var size = CalculateRoomSize(shape);
            var bp = new RoomBlueprint {
                Index = i,
                Shape = shape,
                TileWidth = size.X,
                TileHeight = size.Y,
                IsStart = i == 0,
                IsEnd = i == config.RoomCount - 1,
                ChallengeType = PickChallengeType(i),
            };
            blueprints.Add(bp);
        }

        return blueprints;
    }

    private RoomShape PickRoomShape(int roomIndex) {
        if (roomIndex == 0) return RoomShape.Horizontal; // start room is always simple
        var shapes = Enum.GetValues(typeof(RoomShape));
        return (RoomShape)shapes.GetValue(rng.Next(shapes.Length));
    }

    private Point CalculateRoomSize(RoomShape shape) {
        int difficultyScale = (int)config.Difficulty + 1;
        int baseHeight = config.SideHeight;
        return shape switch {
            RoomShape.Horizontal => new Point(30 + rng.Next(10) * difficultyScale, baseHeight),
            RoomShape.Vertical => new Point(25, baseHeight + rng.Next(10) * difficultyScale),
            RoomShape.LShape => new Point(30 + rng.Next(5), baseHeight + rng.Next(5)),
            RoomShape.Square => new Point(25 + rng.Next(5), baseHeight + rng.Next(5) - 3),
            RoomShape.TallShaft => new Point(20, baseHeight + rng.Next(15) * difficultyScale + 10),
            RoomShape.WideHall => new Point(40 + rng.Next(15) * difficultyScale, Math.Max(15, baseHeight - 5)),
            _ => new Point(30, baseHeight)
        };
    }

    private ChallengeType PickChallengeType(int roomIndex) {
        if (roomIndex == 0) return ChallengeType.Tutorial;
        var types = new[] {
            ChallengeType.Platforming,
            ChallengeType.HazardGauntlet,
            ChallengeType.ClimbingSection,
            ChallengeType.PrecisionJumps,
            ChallengeType.DashPuzzle
        };

        // Weight based on difficulty
        if (config.Difficulty <= Difficulty.Easy)
            return types[rng.Next(2)]; // Only platforming or hazard gauntlet
        return types[rng.Next(types.Length)];
    }

    #endregion

    #region Room Layout

    private void LayoutRooms(List<RoomBlueprint> blueprints) {
        int currentX = 0;
        int currentY = 0;

        for (int i = 0; i < blueprints.Count; i++) {
            var bp = blueprints[i];

            if (config.LinearPath) {
                bp.TileX = currentX;
                bp.TileY = currentY;

                // Alternate between horizontal and vertical movement
                if (i % 2 == 0) {
                    currentX += bp.TileWidth + 1; // 1 tile gap for transitions
                } else {
                    currentY += rng.Next(2) == 0 ? bp.TileHeight + 1 : -(bp.TileHeight + 1);
                    if (currentY < 0) currentY = 0;
                }
            } else {
                // Grid layout for non-linear paths
                int cols = (int)Math.Ceiling(Math.Sqrt(blueprints.Count));
                int row = i / cols;
                int col = i % cols;
                bp.TileX = col * 50;
                bp.TileY = row * 40;
            }
        }
    }

    #endregion

    #region Room Creation

    private Room CreateRoom(RoomBlueprint bp, int index) {
        string name = bp.IsStart ? "a-00" : bp.IsEnd ? $"z-{index:D2}" : $"r-{index:D2}";
        var bounds = new Rectangle(bp.TileX, bp.TileY, bp.TileWidth, bp.TileHeight);
        var room = new Room(name, bounds, map);

        // Set room music based on theme
        room.Music = GetThemeMusic();

        return room;
    }

    private string GetThemeMusic() {
        return config.Theme switch {
            ThemePreset.Forsaken => "event:/music/lvl1/main",
            ThemePreset.OldSite => "event:/music/lvl2/mirror",
            ThemePreset.Resort => "event:/music/lvl3/intro",
            ThemePreset.Ridge => "event:/music/lvl4/main",
            ThemePreset.Temple => "event:/music/lvl5/normal",
            ThemePreset.Reflection => "event:/music/lvl6/main",
            ThemePreset.Summit => "event:/music/lvl7/main",
            ThemePreset.Core => "event:/music/lvl8/main",
            _ => "event:/music/lvl1/main"
        };
    }

    #endregion

    #region Room Population

    private void PopulateRoom(Room room, RoomBlueprint bp, int index) {
        // 1. Generate base terrain (floor, walls, ceiling)
        GenerateBaseTerrain(room, bp);

        // 2. Generate platforms
        GeneratePlatforms(room, bp);

        // 3. Place spawn point in first room
        if (bp.IsStart)
            PlaceSpawnPoint(room);

        // 4. Place hazards based on difficulty
        if (!bp.IsStart)
            PlaceHazards(room, bp);

        // 5. Place collectables
        if (config.IncludeStrawberries && !bp.IsStart)
            PlaceStrawberries(room, bp);

        // 6. Place springs and helpers
        if (config.IncludeSprings && !bp.IsStart)
            PlaceSprings(room, bp);

        // 7. Place crumble blocks for advanced rooms
        if (config.IncludeCrumbleBlocks && config.Difficulty >= Difficulty.Medium && !bp.IsStart)
            PlaceCrumbleBlocks(room, bp);
    }

    private void GenerateBaseTerrain(Room room, RoomBlueprint bp) {
        int w = bp.TileWidth;
        int h = bp.TileHeight;

        // Floor (bottom 2 tiles)
        for (int x = 0; x < w; x++)
            for (int y = h - 2; y < h; y++)
                room.SetFgTile(x, y, fgTileChar);

        // Ceiling (top 1 tile)
        for (int x = 0; x < w; x++)
            room.SetFgTile(x, 0, fgTileChar);

        // Left wall
        for (int y = 0; y < h; y++)
            room.SetFgTile(0, y, fgTileChar);

        // Right wall
        for (int y = 0; y < h; y++)
            room.SetFgTile(w - 1, y, fgTileChar);

        // Shape-specific terrain
        switch (bp.Shape) {
            case RoomShape.LShape:
                // Add the L corner: fill bottom-right quadrant
                int halfW = w / 2;
                int halfH = h / 2;
                for (int x = halfW; x < w; x++)
                    for (int y = 0; y < halfH; y++)
                        room.SetFgTile(x, y, fgTileChar);
                break;

            case RoomShape.TallShaft:
                // Add some ledges to the shaft
                for (int step = 0; step < h / 8; step++) {
                    int ledgeY = 3 + step * 8;
                    bool leftSide = step % 2 == 0;
                    int startX = leftSide ? 1 : w - 5;
                    for (int x = startX; x < startX + 4; x++)
                        if (x > 0 && x < w - 1 && ledgeY < h - 2)
                            room.SetFgTile(x, ledgeY, fgTileChar);
                }
                break;

            case RoomShape.WideHall:
                // Add some pillars
                int pillarSpacing = 8 + rng.Next(4);
                for (int px = pillarSpacing; px < w - 2; px += pillarSpacing) {
                    for (int py = h - 6; py < h - 2; py++)
                        room.SetFgTile(px, py, fgTileChar);
                }
                break;
        }

        // Background tiles (fill everything with bg tile)
        for (int x = 1; x < w - 1; x++)
            for (int y = 1; y < h - 2; y++)
                room.SetBgTile(x, y, bgTileChar);

        // Gaps in the floor for pit hazards (on harder difficulties)
        if (config.Difficulty >= Difficulty.Hard && bp.ChallengeType == ChallengeType.HazardGauntlet) {
            int numGaps = 1 + rng.Next((int)config.Difficulty);
            for (int g = 0; g < numGaps; g++) {
                int gapX = 4 + rng.Next(w - 8);
                int gapWidth = 2 + rng.Next(3);
                for (int dx = 0; dx < gapWidth && gapX + dx < w - 1; dx++) {
                    room.SetFgTile(gapX + dx, h - 2, '0');
                    room.SetFgTile(gapX + dx, h - 1, '0');
                }
            }
        }

        room.Autotile();
    }

    private void GeneratePlatforms(Room room, RoomBlueprint bp) {
        int w = bp.TileWidth;
        int h = bp.TileHeight;

        int numPlatforms = (int)(w * h * config.PlatformDensity / 100f);
        numPlatforms = Math.Max(2, Math.Min(numPlatforms, 15));

        for (int p = 0; p < numPlatforms; p++) {
            int platX = 2 + rng.Next(w - 6);
            int platY = 4 + rng.Next(h - 8);
            int platLen = 2 + rng.Next(4);

            for (int dx = 0; dx < platLen && platX + dx < w - 1; dx++)
                room.SetFgTile(platX + dx, platY, fgTileChar);
        }

        room.Autotile();
    }

    private void PlaceSpawnPoint(Room room) {
        var player = Entity.Create("player", room);
        if (player != null) {
            player.Position = new Vector2(
                (room.X + 3) * 8,
                (room.Y + room.Height - 4) * 8
            );
            player.EntityID = PlacementTool.AllocateId();
            room.AddEntity(player);
        }
    }

    private void PlaceHazards(Room room, RoomBlueprint bp) {
        int numHazards = (int)(bp.TileWidth * config.HazardDensity);
        numHazards = Math.Max(1, Math.Min(numHazards, 20));

        // Place spikes on the floor
        if (config.IncludeSpikes) {
            int spikeCount = numHazards / 2;
            for (int s = 0; s < spikeCount; s++) {
                int spikeX = 3 + rng.Next(bp.TileWidth - 6);
                var spike = Entity.Create("spikesUp", room);
                if (spike != null) {
                    spike.Position = new Vector2(
                        (room.X + spikeX) * 8,
                        (room.Y + room.Height - 3) * 8
                    );
                    spike.Width = 8 * (1 + rng.Next(3));
                    spike.Height = 8;
                    spike.EntityID = PlacementTool.AllocateId();
                    room.AddEntity(spike);
                }
            }
        }

        // Place crystal spinners
        if (config.IncludeSpinners) {
            int spinnerCount = numHazards / 3;
            for (int s = 0; s < spinnerCount; s++) {
                int spinnerX = 3 + rng.Next(bp.TileWidth - 6);
                int spinnerY = 3 + rng.Next(bp.TileHeight - 8);
                var spinner = Entity.Create("spinner", room);
                if (spinner != null) {
                    spinner.Position = new Vector2(
                        (room.X + spinnerX) * 8,
                        (room.Y + spinnerY) * 8
                    );
                    spinner.EntityID = PlacementTool.AllocateId();
                    room.AddEntity(spinner);
                }
            }
        }
    }

    private void PlaceStrawberries(Room room, RoomBlueprint bp) {
        int numBerries = 1 + rng.Next(2 + (int)config.Difficulty);
        numBerries = Math.Min(numBerries, 3);

        for (int b = 0; b < numBerries; b++) {
            int berryX = 3 + rng.Next(bp.TileWidth - 6);
            int berryY = 3 + rng.Next(bp.TileHeight - 8);
            var berry = Entity.Create("strawberry", room);
            if (berry != null) {
                berry.Position = new Vector2(
                    (room.X + berryX) * 8,
                    (room.Y + berryY) * 8
                );
                berry.EntityID = PlacementTool.AllocateId();
                room.AddEntity(berry);
            }
        }
    }

    private void PlaceSprings(Room room, RoomBlueprint bp) {
        int numSprings = 1 + rng.Next(3);

        for (int s = 0; s < numSprings; s++) {
            int springX = 3 + rng.Next(bp.TileWidth - 6);
            var spring = Entity.Create("spring", room);
            if (spring != null) {
                spring.Position = new Vector2(
                    (room.X + springX) * 8,
                    (room.Y + room.Height - 3) * 8
                );
                spring.EntityID = PlacementTool.AllocateId();
                room.AddEntity(spring);
            }
        }
    }

    private void PlaceCrumbleBlocks(Room room, RoomBlueprint bp) {
        int numCrumble = 1 + rng.Next(3);

        for (int c = 0; c < numCrumble; c++) {
            int crumbleX = 3 + rng.Next(bp.TileWidth - 6);
            int crumbleY = 4 + rng.Next(bp.TileHeight - 8);
            var crumble = Entity.Create("crumbleBlock", room);
            if (crumble != null) {
                crumble.Position = new Vector2(
                    (room.X + crumbleX) * 8,
                    (room.Y + crumbleY) * 8
                );
                crumble.Width = 8 * (2 + rng.Next(3));
                crumble.EntityID = PlacementTool.AllocateId();
                room.AddEntity(crumble);
            }
        }
    }

    #endregion

    #region Room Connections

    private void ConnectRooms(List<RoomBlueprint> blueprints) {
        if (blueprints.Count < 2) return;

        // Create openings between adjacent rooms for transitions
        for (int i = 0; i < blueprints.Count - 1; i++) {
            var current = blueprints[i];
            var next = blueprints[i + 1];

            // Determine which wall to open based on relative position
            var currentRoom = map.Rooms[i];
            var nextRoom = map.Rooms[i + 1];

            // Open right wall of current room and left wall of next room for horizontal connection
            if (next.TileX > current.TileX) {
                int openY = Math.Max(current.TileHeight / 2 - 2, 2);
                int openHeight = 4;
                for (int dy = 0; dy < openHeight; dy++) {
                    if (openY + dy > 0 && openY + dy < current.TileHeight - 2) {
                        currentRoom.SetFgTile(current.TileWidth - 1, openY + dy, '0');
                    }
                    if (openY + dy > 0 && openY + dy < next.TileHeight - 2) {
                        nextRoom.SetFgTile(0, openY + dy, '0');
                    }
                }
            }

            // Open bottom/top for vertical connections
            if (next.TileY > current.TileY) {
                int openX = Math.Max(current.TileWidth / 2 - 2, 2);
                int openWidth = 4;
                for (int dx = 0; dx < openWidth; dx++) {
                    if (openX + dx > 0 && openX + dx < current.TileWidth - 1) {
                        currentRoom.SetFgTile(openX + dx, current.TileHeight - 1, '0');
                        currentRoom.SetFgTile(openX + dx, current.TileHeight - 2, '0');
                    }
                    if (openX + dx > 0 && openX + dx < next.TileWidth - 1) {
                        nextRoom.SetFgTile(openX + dx, 0, '0');
                    }
                }
            }

            currentRoom.Autotile();
            nextRoom.Autotile();
        }
    }

    #endregion

    #region Helper Types

    public class RoomBlueprint {
        public int Index;
        public RoomShape Shape;
        public int TileWidth, TileHeight;
        public int TileX, TileY;
        public bool IsStart, IsEnd;
        public ChallengeType ChallengeType;
    }

    public enum ChallengeType {
        Tutorial,
        Platforming,
        HazardGauntlet,
        ClimbingSection,
        PrecisionJumps,
        DashPuzzle
    }

    #endregion
}
