namespace Odemehub.Enums;

/// <summary>
/// How a payment link whose amount the payer picks reads its tax rate
/// against what they pay: split out of it (<see cref="Inclusive"/>: 100 paid
/// is 83.33 and 16.67 of tax at 20%), or added on top of it
/// (<see cref="Exclusive"/>: 100 written is 120 charged). A link of lines
/// keeps the tax inside each line.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum TaxMode
{
    Unknown = 0,
    Inclusive,
    Exclusive,
}
