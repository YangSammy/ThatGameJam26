"""Read-only routing/resource audit; does not start or refresh the Unity Editor."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets"


def check(condition, description):
    if not condition:
        raise AssertionError(description)
    print("PASS " + description)


def main():
    for style in ("Brush", "Noise"):
        settings = ASSETS / f"Resources/{style}SceneTransition.asset"
        check(settings.is_file(), f"{style} settings included through Resources")
        content = settings.read_text(encoding="utf-8-sig")
        for field, suffix in (("transitionIn", "IN"), ("transitionOut", "OUT")):
            prefab = ASSETS / f"EasyTransitions/Transitions/{style}/{style}{suffix}.prefab"
            guid = re.search(r"^guid: (\w+)", Path(str(prefab) + ".meta").read_text(), re.M)[1]
            check(re.search(rf"{field}: .*guid: {guid}\b", content), f"{field} references imported {style}{suffix}")
        for anim_path in (ASSETS / f"EasyTransitions/Transitions/{style}").glob("*.anim"):
            check("m_LoopTime: 0" in anim_path.read_text(), f"{anim_path.stem} is non-looping and can hold its last frame")

    allowed = {"BrushSceneTransition.cs", "WBMain.SpringBedroom.cs", "WBMain.BedroomPreload.cs"}
    for path in (ASSETS / "Scripts").rglob("*.cs"):
        source = path.read_bytes().decode("utf-8", errors="replace")
        if re.search(r"SceneManager\s*\.\s*LoadScene(?:Async)?\s*\(", source):
            check(path.name in allowed, "scene-load ownership: " + path.name)
            if path.name != "BrushSceneTransition.cs":
                check("LoadSceneMode.Additive" in source, "remaining loader is additive: " + path.name)

    menu = (ASSETS / "Scripts/Menu/MainMenuController.cs").read_text(encoding="utf-8-sig")
    for entry in ("NewGame()", "StartGame(bool fresh)"):
        body = menu.split(entry, 1)[1].split("try", 1)[0]
        check("if(busy || BrushSceneTransition.IsBusy)return;" in body, entry + " rejects keyboard submission during Brush")
    check("!BrushSceneTransition.IsBusy && continueButton.interactable" in menu, "Continue rejects keyboard submission during Brush")
    controller = (ASSETS / "Scripts/Core/BrushSceneTransition.cs").read_text()
    check("SceneManager.GetActiveScene()" not in controller, "host readiness does not depend on additive active-scene changes")
    check(controller.index("PrepareSceneDeparture()") < controller.index("SceneManager.LoadSceneAsync"), "held bedroom preload drains before requesting a single-scene load")
    bedroom = (ASSETS / "Scripts/Core/WBMain.BedroomTransition.cs").read_text(encoding="utf-8-sig")
    check(bedroom.index("BrushSceneTransition.Cover()") < bedroom.index("DrainBedroomPreload(destination)"), "bedroom mutation starts after Brush coverage")
    check(bedroom.index("DebugEnterChapter(debugKey)") < bedroom.index("BrushSceneTransition.Reveal()"), "debug object/camera setup precedes reveal")
    check("if(!BrushSceneTransition.ExternalLoadActive)" in bedroom, "external navigation owns its final reveal")
    print("COMPLETE: static routing and resource checks only; visual/runtime acceptance is separate.")


if __name__ == "__main__":
    main()
