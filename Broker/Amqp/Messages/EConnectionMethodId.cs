namespace Broker.Amqp;

public enum EConnectionMethodId : short
{
    Start = 10,
    StartOk = 11,
    Tune = 30,
    TuneOk = 31,
    Open = 40,
    OpenOk = 41,
}
