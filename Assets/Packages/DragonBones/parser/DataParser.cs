/**
 * The MIT License (MIT)
 *
 * Copyright (c) 2012-2017 DragonBones team and other contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
﻿using System.Collections.Generic;

namespace DragonBones
{
    /// <internal/>
    /// <private/>
    public abstract class DataParser
    {
        protected const string DATA_VERSION_2_3 = "2.3";
        protected const string DATA_VERSION_3_0 = "3.0";
        protected const string DATA_VERSION_4_0 = "4.0";
        protected const string DATA_VERSION_4_5 = "4.5";
        protected const string DATA_VERSION_5_0 = "5.0";
        protected const string DATA_VERSION_5_5 = "5.5";
        protected const string DATA_VERSION = DATA_VERSION_5_5;
        protected static readonly List<string> DATA_VERSIONS = new List<string>()
        {
            DATA_VERSION_5_5,
            DATA_VERSION_5_0,
            DATA_VERSION_4_5,
            DATA_VERSION_4_0,
            DATA_VERSION_3_0,
            DATA_VERSION_2_3
        };

        protected const string TEXTURE_ATLAS = "textureAtlas";
        protected const string SUB_TEXTURE = "SubTexture";
        protected const string FORMAT = "format";
        protected const string IMAGE_PATH = "imagePath";
        protected const string WIDTH = "width";
        protected const string HEIGHT = "height";
        protected const string ROTATED = "rotated";
        protected const string FRAME_X = "frameX";
        protected const string FRAME_Y = "frameY";
        protected const string FRAME_WIDTH = "frameWidth";
        protected const string FRAME_HEIGHT = "frameHeight";

        protected const string DRADON_BONES = "dragonBones";
        protected const string USER_DATA = "userData";
        protected const string ARMATURE = "armature";
        protected const string BONE = "bone";
        protected const string SLOT = "slot";
        protected const string CONSTRAINT = "constraint";
        protected const string IK = "ik";
        
        protected const string SKIN = "skin";
        protected const string DISPLAY = "display";
        protected const string ANIMATION = "animation";
        protected const string Z_ORDER = "zOrder";
        protected const string FFD = "ffd";
        protected const string FRAME = "frame";
        protected const string TRANSLATE_FRAME = "translateFrame";
        protected const string ROTATE_FRAME = "rotateFrame";
        protected const string SCALE_FRAME = "scaleFrame";
        protected const string DISPLAY_FRAME = "displayFrame";
        protected const string COLOR_FRAME = "colorFrame";
        protected const string DEFAULT_ACTIONS = "defaultActions";
        protected const string ACTIONS = "actions";
        protected const string EVENTS = "events";
        protected const string INTS = "ints";
        protected const string FLOATS = "floats";
        protected const string STRINGS = "strings";
        protected const string CANVAS = "canvas";

        protected const string TRANSFORM = "transform";
        protected const string PIVOT = "pivot";
        protected const string AABB = "aabb";
        protected const string COLOR = "color";

        protected const string VERSION = "version";
        protected const string COMPATIBLE_VERSION = "compatibleVersion";
        protected const string FRAME_RATE = "frameRate";
        protected const string TYPE = "type";
        protected const string SUB_TYPE = "subType";
        protected const string NAME = "name";
        protected const string PARENT = "parent";
        protected const string TARGET = "target";
        protected const string STAGE = "stage";
        protected const string SHARE = "share";
        protected const string PATH = "path";
        protected const string LENGTH = "length";
        protected const string DISPLAY_INDEX = "displayIndex";
        protected const string BLEND_MODE = "blendMode";
        protected const string INHERIT_TRANSLATION = "inheritTranslation";
        protected const string INHERIT_ROTATION = "inheritRotation";
        protected const string INHERIT_SCALE = "inheritScale";
        protected const string INHERIT_REFLECTION = "inheritReflection";
        protected const string INHERIT_ANIMATION = "inheritAnimation";
        protected const string INHERIT_DEFORM = "inheritDeform";
        protected const string BEND_POSITIVE = "bendPositive";
        protected const string CHAIN = "chain";
        protected const string WEIGHT = "weight";

        protected const string FADE_IN_TIME = "fadeInTime";
        protected const string PLAY_TIMES = "playTimes";
        protected const string SCALE = "scale";
        protected const string OFFSET = "offset";
        protected const string POSITION = "position";
        protected const string DURATION = "duration";
        protected const string TWEEN_TYPE = "tweenType";
        protected const string TWEEN_EASING = "tweenEasing";
        protected const string TWEEN_ROTATE = "tweenRotate";
        protected const string TWEEN_SCALE = "tweenScale";
        protected const string CLOCK_WISE = "clockwise";
        protected const string CURVE = "curve";
        protected const string SOUND = "sound";
        protected const string EVENT = "event";
        protected const string ACTION = "action";

        protected const string X = "x";
        protected const string Y = "y";
        protected const string SKEW_X = "skX";
        protected const string SKEW_Y = "skY";
        protected const string SCALE_X = "scX";
        protected const string SCALE_Y = "scY";
        protected const string VALUE = "value";
        protected const string ROTATE = "rotate";
        protected const string SKEW = "skew";

        protected const string ALPHA_OFFSET = "aO";
        protected const string RED_OFFSET = "rO";
        protected const string GREEN_OFFSET = "gO";
        protected const string BLUE_OFFSET = "bO";
        protected const string ALPHA_MULTIPLIER = "aM";
        protected const string RED_MULTIPLIER = "rM";
        protected const string GREEN_MULTIPLIER = "gM";
        protected const string BLUE_MULTIPLIER = "bM";

        protected const string UVS = "uvs";
        protected const string VERTICES = "vertices";
        protected const string TRIANGLES = "triangles";
        protected const string WEIGHTS = "weights";
        protected const string SLOT_POSE = "slotPose";
        protected const string BONE_POSE = "bonePose";

        protected const string GOTO_AND_PLAY = "gotoAndPlay";

        protected const string DEFAULT_NAME = "default";

        protected static ArmatureType _GetArmatureType(string value)
        {
            switch (value.ToLower())
            {
                case "stage":
                    return ArmatureType.Stage;

                case "armature":
                    return ArmatureType.Armature;

                case "movieclip":
                    return ArmatureType.MovieClip;

                default:
                    return ArmatureType.None;
            }
        }

        protected static DisplayType _GetDisplayType(string value)
        {
            switch (value.ToLower())
            {
                case "image":
                    return DisplayType.Image;

                case "mesh":
                    return DisplayType.Mesh;

                case "armature":
                    return DisplayType.Armature;

                case "boundingbox":
                    return DisplayType.BoundingBox;

                default:
                    return DisplayType.None;
            }
        }

        protected static BoundingBoxType _GetBoundingBoxType(string value)
        {
            switch (value.ToLower())
            {
                case "rectangle":
                    return BoundingBoxType.Rectangle;

                case "ellipse":
                    return BoundingBoxType.Ellipse;

                case "polygon":
                    return BoundingBoxType.Polygon;

                default:
                    return BoundingBoxType.Rectangle;
            }
        }

        protected static ActionType _GetActionType(string value)
        {
            switch (value.ToLower())
            {
                case "play":
                    return ActionType.Play;

                case "frame":
                    return ActionType.Frame;

                case "sound":
                    return ActionType.Sound;

                default:
                    return ActionType.Play;
            }
        }

        protected static BlendMode _GetBlendMode(string value)
        {
            switch (value.ToLower())
            {
                case "normal":
                    return BlendMode.Normal;

                case "add":
                    return BlendMode.Add;

                case "alpha":
                    return BlendMode.Alpha;

                case "darken":
                    return BlendMode.Darken;

                case "difference":
                    return BlendMode.Difference;

                case "erase":
                    return BlendMode.Erase;

                case "hardlight":
                    return BlendMode.HardLight;

                case "invert":
                    return BlendMode.Invert;

                case "layer":
                    return BlendMode.Layer;

                case "lighten":
                    return BlendMode.Lighten;

                case "multiply":
                    return BlendMode.Multiply;

                case "overlay":
                    return BlendMode.Overlay;

                case "screen":
                    return BlendMode.Screen;

                case "subtract":
                    return BlendMode.Subtract;

                default:
                    return BlendMode.Normal;
            }
        }

        public DataParser()
        {

        }

        public abstract DragonBonesData ParseDragonBonesData(object rawData, float scale);

        public abstract bool ParseTextureAtlasData(object rawData, TextureAtlasData textureAtlasData, float scale);
    }
}