using System.Globalization;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public class ContextualAssetResolver
{
    // Is there a better way to do this?
    // Probably
    public static Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>> Resolvers { get; set; }
    public static Dictionary<string, Func<DecompileContext, string, object, string>> VariableResolvers { get; set; }

    private static Dictionary<Enum_EventType, Dictionary<int, string>> _eventSubtypes;
    private static Dictionary<int, string> _blendModes, _gamepadControls;
    private static Dictionary<string, string> _macros;

    public static void Initialize(UndertaleData data)
    {
        // TODO: make this nicer by not hacking
        // into the builtin list
        _eventSubtypes = new Dictionary<Enum_EventType, Dictionary<int, string>>();
        _gamepadControls = new Dictionary<int, string>();
        _blendModes = new Dictionary<int, string>();
        _macros = new Dictionary<string, string>();

        // Don't use
        // Error because of loading audiogroup 
        if (data.GeneralInfo != null)
        {   
            if (data.GeneralInfo.BytecodeVersion <= 14)
            {
                foreach (var constant in data.Options.Constants)
                {
                    if (!constant.Name.Content.StartsWith("@@", StringComparison.Ordinal))
                        _macros[constant.Value.Content] = constant.Name.Content;
                }
            }
        }

        var builtins = data.BuiltinList;
        var constants = builtins.Constants;

        Enum_EventType GetEventTypeFromSubtype(string subtype)
        {
            if (subtype.Contains("gesture", StringComparison.Ordinal))
                return Enum_EventType.ev_gesture;
            if (subtype.Contains("gui", StringComparison.Ordinal) || subtype.Contains("draw", StringComparison.Ordinal)) // DrawGUI events are apparently just prefixed with ev_gui...
                return Enum_EventType.ev_draw;
            if (subtype.Contains("step", StringComparison.Ordinal))
                return Enum_EventType.ev_step;
            // End me
            if (subtype.Contains("user", StringComparison.Ordinal) || subtype.Contains("game_", StringComparison.Ordinal) ||
               subtype.Contains("room_", StringComparison.Ordinal) || subtype.Contains("animation_end", StringComparison.Ordinal) ||
               subtype.Contains("lives", StringComparison.Ordinal) || subtype.Contains("end_of_path", StringComparison.Ordinal) ||
               subtype.Contains("health", StringComparison.Ordinal) || subtype.Contains("close_button", StringComparison.Ordinal) ||
               subtype.Contains("outside", StringComparison.Ordinal) || subtype.Contains("boundary", StringComparison.Ordinal))
                return Enum_EventType.ev_other;

            // ev_close_button is handled above and the various joystick events are 
            // skipped in the loop
            if (subtype.Contains("button", StringComparison.Ordinal) || subtype.Contains("mouse", StringComparison.Ordinal) ||
                subtype.Contains("global", StringComparison.Ordinal) || subtype.Contains("press", StringComparison.Ordinal) ||
                subtype.Contains("release", StringComparison.Ordinal))
                return Enum_EventType.ev_mouse;


            // Note: events with arbitrary subtypes (keyboard, create, precreate, destroy, etc)
            // are not handled here.
            // It also appears to be impossible to manually trigger joystick events?

            // idk what exception to use
            throw new NotImplementedException("No event type for subtype " + subtype);
        }

        Dictionary<int, string> GetDictForEventType(Enum_EventType type)
        {
            // These 3 resolve to the same thing
            if (type == Enum_EventType.ev_keypress || type == Enum_EventType.ev_keyrelease)
                type = Enum_EventType.ev_keyboard;

            if (!_eventSubtypes.ContainsKey(type))
                _eventSubtypes[type] = new Dictionary<int, string>();

            return _eventSubtypes[type];
        }

        // This is going to get bulky really quickly
        foreach (string constant in constants.Keys)
        {
            if (constant.StartsWith("vk_", StringComparison.Ordinal))
                GetDictForEventType(Enum_EventType.ev_keyboard)[(int)constants[constant]] = constant;
            else if (constant.StartsWith("bm_", StringComparison.Ordinal) && !constant.Contains("colour", StringComparison.Ordinal))
                _blendModes[(int)constants[constant]] = constant;
            else if (constant.StartsWith("gp_", StringComparison.Ordinal))
                _gamepadControls[(int)constants[constant]] = constant;
            else if (constant.StartsWith("ev_", StringComparison.Ordinal) && !Enum.IsDefined(typeof(Enum_EventType), constant) && !constant.Contains("joystick", StringComparison.Ordinal))
                GetDictForEventType(GetEventTypeFromSubtype(constant))[(int)constants[constant]] = constant;
        }


        // Uncurse this some time
        Decompiler.ExpressionConstant ConvertToConstExpression(Decompiler.Expression expr)
        {
            if (expr is Decompiler.ExpressionCast)
                expr = (expr as Decompiler.ExpressionCast).Argument;

            if (expr is Decompiler.ExpressionConstant)
                return expr as Decompiler.ExpressionConstant;

            return null;
        }

        int? GetTypeInt(Decompiler.Expression expr)
        {
            var constExpr = ConvertToConstExpression(expr);

            if (constExpr == null)
                return null;

            return AssetTypeResolver.FindConstValue(Decompiler.ExpressionConstant.ConvertToEnumStr<Enum_EventType>(constExpr.Value));
        }

        string resolve_event_perform(DecompileContext context, Decompiler.FunctionCall func, int index, Decompiler.ExpressionConstant self)
        {
            int? typeInt = GetTypeInt(func.Arguments[index - 1]);

            if (typeInt != null)
            {
                Enum_EventType type = (Enum_EventType)typeInt;
                int? initialVal = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                if (initialVal == null)
                    return null;

                int val = initialVal.Value;

                var subtypes = _eventSubtypes;
                if (type == Enum_EventType.ev_collision && val >= 0 && val < data.GameObjects.Count)
                    return data.GameObjects[val].Name.Content;
                else if (type == Enum_EventType.ev_keyboard || type == Enum_EventType.ev_keypress || type == Enum_EventType.ev_keyrelease)
                {
                    string key = self.GetAsKeyboard(context);
                    if (key != null)
                        return key;
                }
                else if (subtypes.ContainsKey(type))
                {
                    var mappings = subtypes[type];
                    if (mappings.ContainsKey(val))
                        return mappings[val];
                }
            }

            return null;
        }

        // TODO: Finish context-dependent variable resolution
        VariableResolvers = new Dictionary<string, Func<DecompileContext, string, object, string>>()
        {
            { "scr_getbuttonsprite", (context, varname, value) =>
                {
                    return null;
                }
            }
        };

        Resolvers = new Dictionary<string, Func<DecompileContext, Decompiler.FunctionCall, int, Decompiler.ExpressionConstant, string>>()
        {
            // TODO: __background* compatibility scripts
            { "event_perform", resolve_event_perform },
            { "event_perform_object", resolve_event_perform },
            { "draw_set_blend_mode", (context, func, index, self) =>
                {
                    int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (val != null)
                    {
                        switch(val)
                        {
                            case 0: return "bm_normal";
                            case 1: return "bm_add";
                            case 2: return "bm_max";
                            case 3: return "bm_subtract";
                        }
                    }
                    return null;
                }
            },
            { "gpu_set_blendmode", (context, func, index, self) =>
                {
                    int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (val != null)
                    {
                        switch(val)
                        {
                            case 0: return "bm_normal";
                            case 1: return "bm_add";
                            case 2: return "bm_max";
                            case 3: return "bm_subtract";
                        }
                    }
                    return null;
                }
            },
            { "draw_set_blend_mode_ext", (context, func, index, self) =>
                {
                    int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (val == null)
                        return null;

                    return _blendModes.ContainsKey(val.Value) ? _blendModes[val.Value] : null;
                }
            },
            { "gpu_set_blendmode_ext", (context, func, index, self) =>
                {
                    int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);
                    if (val == null)
                        return null;

                    return _blendModes.ContainsKey(val.Value) ? _blendModes[val.Value] : null;
                }
            },
            { "__view_set", (context, func, index, self) => 
                {
                    var first = ConvertToConstExpression(func.Arguments[0]);
                    if (first == null)
                        return null;

                    int type = Decompiler.ExpressionConstant.ConvertToInt(first.Value) ?? -1;
                    int? val = Decompiler.ExpressionConstant.ConvertToInt(self.Value);

                    if (val == null)
                        return null;

                    switch(type)
                    {
                        case 9:
                            {
                                if (val < 0)
                                    return ((UndertaleInstruction.InstanceType)self.Value).ToString().ToLower(CultureInfo.InvariantCulture);
                                else if (val < data.GameObjects.Count)
                                    return data.GameObjects[val.Value].Name.Content;
                                
                            } break;

                        case 10:
                            {
                                if (val == 0)
                                    return "false";
                                else if (val == 1)
                                    return "true";
                            } break;
                    }
                    return null;
                }
            },
        };
    }
}
