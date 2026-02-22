using Microsoft.Xna.Framework;
using Monocle;

namespace Snowberry.Editor;

/// <summary>
/// Renders a grid overlay in the map editor for alignment assistance.
/// Toggle with Ctrl+G in settings, or via the Settings.
/// </summary>
public static class GridOverlay {

    public static bool Enabled { get; set; } = false;
    public static int CellSize { get; set; } = 8; // 8px = 1 tile
    public static float Opacity { get; set; } = 0.15f;
    public static Color GridColor { get; set; } = Color.White;
    public static Color MajorGridColor { get; set; } = Color.Yellow;
    public static int MajorGridInterval { get; set; } = 5; // every 5 tiles = 40px

    /// <summary>
    /// Renders the grid overlay within the current room or viewport.
    /// Call from Editor.RenderContent() after the map renders.
    /// </summary>
    public static void Render(Editor.BufferCamera camera) {
        if (!Enabled) return;

        Rectangle view = camera.ViewRect;

        // Expand view slightly to cover edges
        int startX = (view.Left / CellSize - 1) * CellSize;
        int startY = (view.Top / CellSize - 1) * CellSize;
        int endX = (view.Right / CellSize + 2) * CellSize;
        int endY = (view.Bottom / CellSize + 2) * CellSize;

        Draw.SpriteBatch.Begin(
            Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred,
            Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
            Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp,
            Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
            Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone,
            null,
            camera.Matrix
        );

        // Vertical lines
        for (int x = startX; x <= endX; x += CellSize) {
            int tileX = x / CellSize;
            bool isMajor = MajorGridInterval > 0 && tileX % MajorGridInterval == 0;
            Color lineColor = (isMajor ? MajorGridColor : GridColor) * Opacity;
            Draw.Line(new Vector2(x, startY), new Vector2(x, endY), lineColor);
        }

        // Horizontal lines
        for (int y = startY; y <= endY; y += CellSize) {
            int tileY = y / CellSize;
            bool isMajor = MajorGridInterval > 0 && tileY % MajorGridInterval == 0;
            Color lineColor = (isMajor ? MajorGridColor : GridColor) * Opacity;
            Draw.Line(new Vector2(startX, y), new Vector2(endX, y), lineColor);
        }

        Draw.SpriteBatch.End();
    }
}
