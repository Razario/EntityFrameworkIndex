using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EntityFrameworkIndex
{
    /// <summary>
    /// Атрибут, указывающий, что по данному полю будет построен индекс
    /// Индекс может быть построен по полю, длина которого не превышает 900 байт.
    /// Внимание! В случае типа поля String необходимо указать атрибут StringLength с максимальной длиной,
    /// не превышающей 900 байт.
    /// </summary>
    public class IndexAttribute : Attribute
    {
    }
}
