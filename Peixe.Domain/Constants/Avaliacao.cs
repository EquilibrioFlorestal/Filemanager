using System;

namespace Domain.Constants
{
    public class Avaliacao
    {
        public static Dictionary<string, List<string>> NomePastaAvaliacao = new Dictionary<string, List<string>>
        {
            { "Colheita", ["Colheita"] },
            { "Formiga Manual", ["Formiga Manual e Operacional"]},
            { "Formiga Mecanizada", ["Formiga Mecanizada"] },
            { "Irrigacao", ["Irrigacao"] },
            { "Adubação de Base de Precisão e Pulverização", ["Adubação de Base de Precisão e Pulverização"] },
            { "Pulverizador", ["Pulverizador"] },
            { "Pulverizacao", ["Pulverizacao"] },
            { "Preparo de Solo", ["Preparo de Solo"] },
            { "Plantio Operacional", ["Plantio Operacional"] },
            { "Baldeio", ["Baldeio"] },
            { "Adubação cobertura", ["Adubação de cobertura Mecanizada"] },
            { "90 dias", ["Sobrevivencia", "90 Dias"] },
            { "30 dias", ["Sobrevivencia", "30 Dias"] }
        };
    }
}
