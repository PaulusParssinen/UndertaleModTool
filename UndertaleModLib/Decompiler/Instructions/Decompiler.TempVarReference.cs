namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // A reference class for tempvars.
    public sealed class TempVarReference
    {
        public TempVar Var;

        public TempVarReference(TempVar var)
        {
            Var = var;
        }
    }
}