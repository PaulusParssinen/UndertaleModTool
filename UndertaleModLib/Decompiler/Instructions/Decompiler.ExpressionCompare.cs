using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a binary comparison expression.
    public sealed class ExpressionCompare : Expression
    {
        public UndertaleInstruction.ComparisonType Opcode;
        public Expression Argument1;
        public Expression Argument2;

        public ExpressionCompare(UndertaleInstruction.ComparisonType opcode, Expression argument1, Expression argument2)
        {
            Opcode = opcode;
            Type = UndertaleInstruction.DataType.Boolean;
            Argument1 = argument1;
            Argument2 = argument2;
        }

        internal override bool IsDuplicationSafe()
        {
            return Argument1.IsDuplicationSafe() && Argument2.IsDuplicationSafe();
        }

        public override string ToString(DecompileContext context)
        {
            string arg1 = Argument1.ToString(context);
            if (Argument1 is ExpressionCompare compare1)
                arg1 = compare1.ToStringWithParen(context);
            string arg2 = Argument2.ToString(context);
            if (Argument2 is ExpressionCompare compare2)
                arg2 = compare2.ToStringWithParen(context);

            return $"{arg1} {OperationToPrintableString(Opcode)} {arg2}";
        }

        public string ToStringWithParen(DecompileContext context)
        {
            return $"({ToString(context)})";
        }

        public override Statement CleanStatement(DecompileContext context, BlockHLStatement block)
        {
            Argument1 = Argument1?.CleanExpression(context, block);
            Argument2 = Argument2?.CleanExpression(context, block);
            return this;
        }

        internal override AssetIDType DoTypePropagation(DecompileContext context, AssetIDType suggestedType)
        {
            // TODO: This should be probably able to go both ways...
            Argument2.DoTypePropagation(context, Argument1.DoTypePropagation(context, suggestedType));

            /*
                if (Opcode != UndertaleInstruction.ComparisonType.EQ && Opcode != UndertaleInstruction.ComparisonType.NEQ)
                {
                    if (Argument1 is ExpressionConstant arg1)
                        if (arg1.AssetType == AssetIDType.Script)
                            arg1.AssetType = AssetIDType.Other;

                    if (Argument2 is ExpressionConstant arg2)
                        if (arg2.AssetType == AssetIDType.Script)
                            arg2.AssetType = AssetIDType.Other;
                }*/

            return AssetIDType.Other;
        }
    }
}