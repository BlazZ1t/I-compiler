using ImperativeLang.SyntaxAnalyzer;

namespace ImperativeLang.SemanticalAnalyzerNS
{
    abstract class TypeInfo
    {
        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public abstract override string ToString();
    }

    class PrimitiveTypeInfo : TypeInfo
    {
        public PrimitiveType Type { get; set; }

        public PrimitiveTypeInfo(PrimitiveType type)
        {
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            return obj is PrimitiveTypeInfo other && Type == other.Type;
        }

        public override int GetHashCode() => Type.GetHashCode();

        public override string ToString()
        {
            return Type.ToString().ToLower();
        }
    }

    class ArrayTypeInfo : TypeInfo
    {
        public TypeInfo ElementType { get; set; }
        public int Size { get; set; }
        public string Name { get; set; }

        public ArrayTypeInfo(TypeInfo type, int size, string name)
        {
            ElementType = type;
            Size = size;
            Name = name;
        }

        public override bool Equals(object? obj)
        {
            return obj is ArrayTypeInfo other &&
                Size == other.Size &&
                ElementType.Equals(other.ElementType) && Name == other.Name;
        }

        public override int GetHashCode() => HashCode.Combine(ElementType, Size, Name);

        public override string ToString()
        {
            return Name;
        }
    }

    class RecordTypeInfo : TypeInfo
    {
        public string Name { get; set; } //Just look up in symbol table for fields

        public Dictionary<string, TypeInfo> Fields { get; set; }  // MAYBE ADD THIS
        public RecordTypeInfo(string name, Dictionary<string, TypeInfo> fields)
        {
            Name = name;
            Fields = fields;
        }

        public override bool Equals(object? obj)
        {
            return obj is RecordTypeInfo other && Name == other.Name;
        }

        public override int GetHashCode() => Name.GetHashCode();

        public override string ToString()
        {
            return Name;
        }
    }
}