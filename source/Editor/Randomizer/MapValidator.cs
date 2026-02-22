using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Snowberry.Editor.Randomizer;

/// <summary>
/// Validates generated maps for playability and correctness.
/// </summary>
public static class MapValidator {

    public class ValidationResult {
        public bool IsValid => Errors.Count == 0;
        public List<ValidationError> Errors { get; } = new();
        public List<ValidationWarning> Warnings { get; } = new();

        public override string ToString() {
            string result = IsValid ? "Map is valid" : $"Map has {Errors.Count} error(s)";
            if (Warnings.Count > 0) result += $" and {Warnings.Count} warning(s)";
            return result;
        }
    }

    public class ValidationError {
        public string RoomName { get; set; }
        public string Message { get; set; }
        public ErrorType Type { get; set; }
    }

    public class ValidationWarning {
        public string RoomName { get; set; }
        public string Message { get; set; }
    }

    public enum ErrorType {
        NoSpawnPoint,
        OverlappingRooms,
        EmptyRoom,
        InvalidRoomSize,
        UnreachableRoom,
        MissingTransition,
        EntityOutOfBounds,
        DuplicateEntityId
    }

    /// <summary>
    /// Validates the given map for common issues.
    /// </summary>
    public static ValidationResult Validate(Map map) {
        var result = new ValidationResult();

        if (map.Rooms.Count == 0) {
            result.Errors.Add(new ValidationError {
                Message = "Map has no rooms",
                Type = ErrorType.EmptyRoom
            });
            return result;
        }

        CheckSpawnPoint(map, result);
        CheckOverlappingRooms(map, result);
        CheckRoomSizes(map, result);
        CheckEntitiesInBounds(map, result);
        CheckDuplicateEntityIds(map, result);
        CheckEmptyRooms(map, result);

        return result;
    }

    private static void CheckSpawnPoint(Map map, ValidationResult result) {
        bool hasSpawn = map.Rooms.Any(r =>
            r.TrackedEntities.TryGetValue(typeof(Entities.Plugin_Player), out var players) && players.Any());

        if (!hasSpawn) {
            result.Errors.Add(new ValidationError {
                Message = "Map has no spawn point (player entity)",
                Type = ErrorType.NoSpawnPoint
            });
        }
    }

    private static void CheckOverlappingRooms(Map map, ValidationResult result) {
        for (int i = 0; i < map.Rooms.Count; i++) {
            for (int j = i + 1; j < map.Rooms.Count; j++) {
                var a = map.Rooms[i];
                var b = map.Rooms[j];

                Rectangle rectA = new(a.X * 8, a.Y * 8, a.Width * 8, a.Height * 8);
                Rectangle rectB = new(b.X * 8, b.Y * 8, b.Width * 8, b.Height * 8);

                if (rectA.Intersects(rectB)) {
                    result.Errors.Add(new ValidationError {
                        RoomName = $"{a.Name} & {b.Name}",
                        Message = $"Rooms '{a.Name}' and '{b.Name}' overlap",
                        Type = ErrorType.OverlappingRooms
                    });
                }
            }
        }
    }

    private static void CheckRoomSizes(Map map, ValidationResult result) {
        foreach (var room in map.Rooms) {
            if (room.Width < 5 || room.Height < 5) {
                result.Errors.Add(new ValidationError {
                    RoomName = room.Name,
                    Message = $"Room '{room.Name}' is too small ({room.Width}x{room.Height} tiles, minimum 5x5)",
                    Type = ErrorType.InvalidRoomSize
                });
            }

            if (room.Width > 200 || room.Height > 200) {
                result.Warnings.Add(new ValidationWarning {
                    RoomName = room.Name,
                    Message = $"Room '{room.Name}' is very large ({room.Width}x{room.Height} tiles)"
                });
            }
        }
    }

    private static void CheckEntitiesInBounds(Map map, ValidationResult result) {
        foreach (var room in map.Rooms) {
            Rectangle roomPixelBounds = new(room.X * 8, room.Y * 8, room.Width * 8, room.Height * 8);

            foreach (var entity in room.AllEntities) {
                if (!roomPixelBounds.Contains((int)entity.Position.X, (int)entity.Position.Y)) {
                    result.Warnings.Add(new ValidationWarning {
                        RoomName = room.Name,
                        Message = $"Entity '{entity.Name}' (ID {entity.EntityID}) is outside room '{room.Name}' bounds"
                    });
                }
            }
        }
    }

    private static void CheckDuplicateEntityIds(Map map, ValidationResult result) {
        var allEntities = map.Rooms.SelectMany(r => r.AllEntities).ToList();
        var idGroups = allEntities.GroupBy(e => e.EntityID).Where(g => g.Count() > 1);

        foreach (var group in idGroups) {
            if (group.Key == 0) continue; // ID 0 is a common default

            result.Warnings.Add(new ValidationWarning {
                Message = $"Multiple entities share ID {group.Key}: {string.Join(", ", group.Select(e => e.Name))}"
            });
        }
    }

    private static void CheckEmptyRooms(Map map, ValidationResult result) {
        foreach (var room in map.Rooms) {
            if (room.AllEntities.Count == 0) {
                result.Warnings.Add(new ValidationWarning {
                    RoomName = room.Name,
                    Message = $"Room '{room.Name}' has no entities"
                });
            }
        }
    }
}
