using System;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using Snowberry.Editor.Randomizer;
using Snowberry.UI;
using Snowberry.UI.Controls;
using Snowberry.UI.Layout;

namespace Snowberry.Editor.Tools;

/// <summary>
/// Tool for procedural map generation with configurable parameters.
/// </summary>
public class GeneratorTool : Tool {

    private const int width = 260;

    private ProceduralGenerator.GeneratorConfig config = new();
    private string lastValidationMessage = "";
    private Color lastValidationColor = Color.White;

    // UI references for dynamic updates
    private UIElement panel;
    private UIScrollPane scrollPane;

    public override string GetName() => Dialog.Clean("SNOWBERRY_EDITOR_TOOL_GENERATOR");

    public override UIElement CreatePanel(int height) {
        config = new ProceduralGenerator.GeneratorConfig();

        panel = new UIElement {
            Width = width,
            Background = Calc.HexToColor("202929") * (185 / 255f),
            GrabsClick = true,
            GrabsScroll = true,
            Height = height
        };

        scrollPane = new UIScrollPane {
            Width = width,
            Background = null,
            Height = height - 10
        };
        panel.Add(scrollPane);

        BuildConfigUI();

        return panel;
    }

    private void BuildConfigUI() {
        scrollPane.Clear(now: true);

        var yOffset = new Vector2(5, 5);

        // === Title ===
        var title = new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_TITLE")) {
            FG = Color.Gold
        };
        scrollPane.AddBelow(title, yOffset);

        // === Seed ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_SEED")), yOffset);
        var seedField = new UIValueTextField<int>(Fonts.Regular, width - 20, "0");
        seedField.OnValidInputChange = v => config.Seed = v;
        scrollPane.AddBelow(seedField, new Vector2(5, 2));

        // === Room Count ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_ROOMS")), yOffset);
        var roomSlider = new UISlider {
            Width = width - 20,
            Min = 2,
            Max = 25,
            Value = config.RoomCount,
            OnInputChanged = v => config.RoomCount = (int)v
        };
        scrollPane.AddBelow(roomSlider, new Vector2(5, 2));

        // === Difficulty ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_DIFFICULTY")), yOffset);
        var diffDropdown = UIDropdown.OfEnum<ProceduralGenerator.Difficulty>(
            Fonts.Regular,
            v => config.Difficulty = v
        );
        scrollPane.AddBelow(diffDropdown, new Vector2(5, 2));

        // === Theme ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_THEME")), yOffset);
        var themeDropdown = UIDropdown.OfEnum<ProceduralGenerator.ThemePreset>(
            Fonts.Regular,
            v => config.Theme = v
        );
        scrollPane.AddBelow(themeDropdown, new Vector2(5, 2));

        // === Layout ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_LAYOUT")), yOffset);
        var linearCheck = new UICheckBox(-1, config.LinearPath);
        linearCheck.OnPress = v => config.LinearPath = v;
        scrollPane.AddBelow(linearCheck, new Vector2(5, 2));

        // === Content Options Header ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_CONTENT")) {
            FG = Color.LightGray
        }, new Vector2(5, 10));

        // === Strawberries ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_STRAWBERRIES")), yOffset);
        var berryCheck = new UICheckBox(-1, config.IncludeStrawberries);
        berryCheck.OnPress = v => config.IncludeStrawberries = v;
        scrollPane.AddBelow(berryCheck, new Vector2(5, 2));

        // === Spinners ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_SPINNERS")), yOffset);
        var spinnerCheck = new UICheckBox(-1, config.IncludeSpinners);
        spinnerCheck.OnPress = v => config.IncludeSpinners = v;
        scrollPane.AddBelow(spinnerCheck, new Vector2(5, 2));

        // === Spikes ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_SPIKES")), yOffset);
        var spikeCheck = new UICheckBox(-1, config.IncludeSpikes);
        spikeCheck.OnPress = v => config.IncludeSpikes = v;
        scrollPane.AddBelow(spikeCheck, new Vector2(5, 2));

        // === Springs ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_SPRINGS")), yOffset);
        var springCheck = new UICheckBox(-1, config.IncludeSprings);
        springCheck.OnPress = v => config.IncludeSprings = v;
        scrollPane.AddBelow(springCheck, new Vector2(5, 2));

        // === Crumble Blocks ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_CRUMBLE")), yOffset);
        var crumbleCheck = new UICheckBox(-1, config.IncludeCrumbleBlocks);
        crumbleCheck.OnPress = v => config.IncludeCrumbleBlocks = v;
        scrollPane.AddBelow(crumbleCheck, new Vector2(5, 2));

        // === Density Settings ===
        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_DENSITY")) {
            FG = Color.LightGray
        }, new Vector2(5, 10));

        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_HAZARD_DENSITY")), yOffset);
        var hazardSlider = new UISlider {
            Width = width - 20,
            Min = 0,
            Max = 100,
            Value = config.HazardDensity * 100f,
            OnInputChanged = v => config.HazardDensity = v / 100f
        };
        scrollPane.AddBelow(hazardSlider, new Vector2(5, 2));

        scrollPane.AddBelow(new UILabel(Dialog.Clean("SNOWBERRY_GENERATOR_PLATFORM_DENSITY")), yOffset);
        var platSlider = new UISlider {
            Width = width - 20,
            Min = 0,
            Max = 100,
            Value = config.PlatformDensity * 100f,
            OnInputChanged = v => config.PlatformDensity = v / 100f
        };
        scrollPane.AddBelow(platSlider, new Vector2(5, 2));

        // === Generate Button ===
        var generateBtn = new UIButton(Dialog.Clean("SNOWBERRY_GENERATOR_GENERATE"), Fonts.Regular, 6, 4) {
            OnPress = DoGenerate,
            BG = Calc.HexToColor("2a7d27"),
            HoveredBG = Calc.HexToColor("35a830"),
            PressedBG = Calc.HexToColor("1a5c18")
        };
        scrollPane.AddBelow(generateBtn, new Vector2(5, 15));

        // === Validate Button ===
        var validateBtn = new UIButton(Dialog.Clean("SNOWBERRY_GENERATOR_VALIDATE"), Fonts.Regular, 6, 4) {
            OnPress = DoValidate,
            BG = Calc.HexToColor("2766a8"),
            HoveredBG = Calc.HexToColor("3080cc"),
            PressedBG = Calc.HexToColor("1a4a7d")
        };
        scrollPane.AddBelow(validateBtn, new Vector2(5, 5));

        // === Validation Result ===
        scrollPane.AddBelow(new UILabel(() => lastValidationMessage) {
            FG = lastValidationColor
        }, new Vector2(5, 5));

        scrollPane.CalculateBounds();
    }

    private void DoGenerate() {
        try {
            if (config.Seed == 0)
                config.Seed = new Random().Next(1, 999999);

            var generator = new ProceduralGenerator(config);
            var newMap = generator.Generate();

            // Validate the generated map
            var validation = MapValidator.Validate(newMap);

            if (!validation.IsValid) {
                lastValidationMessage = $"Generation issues: {string.Join("; ", validation.Errors)}";
                lastValidationColor = Color.OrangeRed;
                Snowberry.Log(Celeste.Mod.LogLevel.Warn, lastValidationMessage);
            } else {
                lastValidationMessage = Dialog.Clean("SNOWBERRY_GENERATOR_SUCCESS");
                lastValidationColor = Color.LimeGreen;
            }

            // Open the generated map in the editor
            Engine.Scene = new Editor(newMap);

            Snowberry.LogInfo($"Generated procedural map with seed {config.Seed}");
        } catch (Exception ex) {
            lastValidationMessage = $"Error: {ex.Message}";
            lastValidationColor = Color.Red;
            Snowberry.Log(Celeste.Mod.LogLevel.Error, $"Procedural generation failed: {ex}");
        }
    }

    private void DoValidate() {
        if (Editor.Instance?.Map == null) {
            lastValidationMessage = Dialog.Clean("SNOWBERRY_GENERATOR_NO_MAP");
            lastValidationColor = Color.Yellow;
            return;
        }

        var result = MapValidator.Validate(Editor.Instance.Map);

        if (result.IsValid && result.Warnings.Count == 0) {
            lastValidationMessage = Dialog.Clean("SNOWBERRY_GENERATOR_VALID");
            lastValidationColor = Color.LimeGreen;
        } else if (result.IsValid) {
            lastValidationMessage = $"{result.Warnings.Count} warning(s)";
            lastValidationColor = Color.Yellow;
        } else {
            lastValidationMessage = $"{result.Errors.Count} error(s)";
            lastValidationColor = Color.Red;
        }
    }

    public override void Update(bool canClick) {
        // No special keybinds needed - Ctrl+G is handled by the editor for grid toggle
    }

    public override void RenderWorldSpace() { }

    public override void RenderScreenSpace() { }

    public override void ResizePanel(int height) {
        if (panel != null) panel.Height = height;
        if (scrollPane != null) scrollPane.Height = height - 10;
    }
}
