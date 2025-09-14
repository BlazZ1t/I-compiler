public enum TokenType
{
    // Keywords
    Var,
    Type,
    Is,
    Record,
    End,
    Array,
    While,
    Loop,
    For,
    In,
    Reverse,
    If,
    Then,
    Else,
    Print,
    Routine,
    Return,
    True,
    False,
    Not,
    And,
    Or,
    Xor,

    // Primitive types
    Integer,
    Real,
    Boolean,

    // Identifiers & Literals
    Identifier,
    IntegerLiteral,
    RealLiteral,

    // Operators
    Assign,        // :=
    Plus,          // +
    Minus,         // -
    Multiply,      // *
    Divide,        // /
    Modulo,        // %
    Less,          // <
    LessEqual,     // <=
    Greater,       // >
    GreaterEqual,  // >=
    Equal,         // =
    NotEqual,      // /=
    RoutineExpression, // =>

    // Delimiters
    LParen,        // (
    RParen,        // )
    LBracket,      // [
    RBracket,      // ]
    Dot,           // .
    Comma,         // ,
    Semicolon,     // ;
    Colon,         // :

    // Special
    EOF,
    NewLine,
    Error
}
