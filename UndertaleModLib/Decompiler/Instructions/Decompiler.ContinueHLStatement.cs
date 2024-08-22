namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    public sealed class ContinueHLStatement : HLStatement
    {
        public override string ToString(DecompileContext context)
        {
            return "continue";
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            return this;
        }
    }
}