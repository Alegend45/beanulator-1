namespace Beanulator.Common
{
    public interface IInputProvider
    {
        bool GetButtonState(int button);
        void Update();
    }
}
