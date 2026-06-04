public interface IDamageReceiver
{
    void TakeDamage(float amount);
    DamageResult ReceiveDamage(DamageContext context);
}
