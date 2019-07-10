using System.Text;

namespace Interfaces
{
    public interface IInitNecessity
    {
        bool IsInitExist { get; }

        StringBuilder GetInit();
    }
}
