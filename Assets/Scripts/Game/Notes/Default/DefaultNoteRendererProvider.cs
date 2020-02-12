using System.Collections.Generic;
using System.Linq;
using DragonBones;
using UnityEngine;
using UnityEngine.Serialization;

public class DefaultNoteRendererProvider : SingletonMonoBehavior<DefaultNoteRendererProvider>
{

    public UnityDragonBonesData ClickDragonBonesData;
    [HideInInspector] public Dictionary<Color, string> DragonBonesDataColorVariants = new Dictionary<Color, string>();

    public string GetDragonBonesDataColorVariant(NoteType noteType, Color color)
    {
        if (DragonBonesDataColorVariants.ContainsKey(color)) return DragonBonesDataColorVariants[color];
        Debug.Log("Warning: Color not found");
        return DragonBonesDataColorVariants.First().Value;
    }
    
    protected void Start()
    {
        if (false)
        {
            var colors = new[] {"#35A7FF".ToColor(), "#FF5964".ToColor()};
            for (var i = 0; i < colors.Length; i++)
            {
                var color = colors[i];
                ClickDragonBonesData.dataName = "Click" + i;
                UnityFactory.factory.LoadData(ClickDragonBonesData);
                DragonBonesDataColorVariants[color] = ClickDragonBonesData.dataName;

                var tmp = UnityFactory.factory.BuildArmatureComponent("Armature", ClickDragonBonesData.dataName);
                var anim = tmp.animation.animations["1a"];
                var dbData = tmp.armature.armatureData.parent;
                new[] {"底1", "底", "圈3"}.ForEach(compName =>
                {
                    var bgData = anim.slotTimelines[compName];

                    bgData.Find(it => it.type == TimelineType.SlotColor).Apply(timelineData =>
                    {
                        for (var frameIndex = 0; frameIndex <= 1; frameIndex++)
                        {
                            var frameIntOffset = anim.frameIntOffset;
                            var frameValueOffset =
                                dbData.timelineArray[timelineData.offset + (int) BinaryOffset.TimelineFrameValueOffset];
                            var valueOffset = frameIntOffset + frameValueOffset + frameIndex * 1;
                            var colorOffset = dbData.frameIntArray[valueOffset];

                            var hslColor = color.ToHslColor();
                            hslColor.L *= 1.2f;
                            var rgbColor = hslColor.ToRgbColor();

                            dbData.intArray[colorOffset + 1] = (short) (rgbColor.r * 100);
                            dbData.intArray[colorOffset + 2] = (short) (rgbColor.g * 100);
                            dbData.intArray[colorOffset + 3] = (short) (rgbColor.b * 100);
                        }
                    });
                });

                Destroy(tmp.gameObject);
            }
        }
        else
        {
            UnityFactory.factory.LoadData(ClickDragonBonesData);
        }
    }
}