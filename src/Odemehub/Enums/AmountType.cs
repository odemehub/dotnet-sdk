namespace Odemehub.Enums;

/// <summary>
/// What a payment link lets the payer pay: the lines the merchant wrote
/// (<see cref="Fixed"/>), any amount they write themselves
/// (<see cref="Custom"/>), one of the amounts offered
/// (<see cref="Predefined"/>), or one of those or an amount of their own
/// (<see cref="PredefinedAndCustom"/>).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> stands for a value this version of the client does
/// not know yet: the gateway may grow a new one, and an answer carrying it is
/// still read rather than turned down. It is never sent.
/// </remarks>
public enum AmountType
{
    Unknown = 0,
    Fixed,
    Custom,
    Predefined,
    PredefinedAndCustom,
}
