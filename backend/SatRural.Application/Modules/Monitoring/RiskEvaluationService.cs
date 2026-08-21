using SatRural.Domain.Common;

namespace SatRural.Application.Modules.Monitoring.Services;

public class RiskEvaluationService
{
    public RiskEvaluationResult Evaluate(
        decimal temperature,
        decimal humidity,
        decimal windSpeed,
        decimal rainfall,
        decimal riverLevel)
    {
        var results = new List<RiskEvaluationResult>
        {
            EvaluateTemperature(temperature),
            EvaluateHumidity(humidity),
            EvaluateWind(windSpeed),
            EvaluateRainfall(rainfall),
            EvaluateRiverLevel(riverLevel)
        };

        return results
            .OrderByDescending(result => result.Level)
            .First();
    }


    private static RiskEvaluationResult EvaluateTemperature(
        decimal value)
    {
        if (value >= 38)
        {
            return Result(
                RiskLevel.Red,
                "Temperatura extremadamente alta."
            );
        }

        if (value >= 35)
        {
            return Result(
                RiskLevel.Orange,
                "Temperatura elevada."
            );
        }

        if (value >= 32)
        {
            return Result(
                RiskLevel.Yellow,
                "Temperatura por encima del rango normal."
            );
        }

        if (value <= 0)
        {
            return Result(
                RiskLevel.Red,
                "Condiciones críticas de helada."
            );
        }

        if (value <= 4)
        {
            return Result(
                RiskLevel.Orange,
                "Riesgo elevado de helada."
            );
        }

        if (value <= 8)
        {
            return Result(
                RiskLevel.Yellow,
                "Temperatura baja. Condiciones de precaución."
            );
        }

        return Result(
            RiskLevel.Green,
            "Temperatura dentro del rango normal."
        );
    }


    private static RiskEvaluationResult EvaluateHumidity(
        decimal value)
    {
        if (value <= 20)
        {
            return Result(
                RiskLevel.Red,
                "Humedad extremadamente baja."
            );
        }

        if (value <= 30)
        {
            return Result(
                RiskLevel.Orange,
                "Humedad muy baja."
            );
        }

        if (value <= 40)
        {
            return Result(
                RiskLevel.Yellow,
                "Humedad baja."
            );
        }

        return Result(
            RiskLevel.Green,
            "Humedad dentro del rango normal."
        );
    }


    private static RiskEvaluationResult EvaluateWind(
        decimal value)
    {
        if (value >= 70)
        {
            return Result(
                RiskLevel.Red,
                "Vientos extremadamente fuertes."
            );
        }

        if (value >= 50)
        {
            return Result(
                RiskLevel.Orange,
                "Vientos fuertes."
            );
        }

        if (value >= 30)
        {
            return Result(
                RiskLevel.Yellow,
                "Incremento considerable del viento."
            );
        }

        return Result(
            RiskLevel.Green,
            "Velocidad del viento dentro del rango normal."
        );
    }


    private static RiskEvaluationResult EvaluateRainfall(
        decimal value)
    {
        if (value >= 50)
        {
            return Result(
                RiskLevel.Red,
                "Precipitación extrema."
            );
        }

        if (value >= 30)
        {
            return Result(
                RiskLevel.Orange,
                "Lluvia muy intensa."
            );
        }

        if (value >= 15)
        {
            return Result(
                RiskLevel.Yellow,
                "Lluvia intensa en observación."
            );
        }

        return Result(
            RiskLevel.Green,
            "Nivel de lluvia dentro del rango normal."
        );
    }


    private static RiskEvaluationResult EvaluateRiverLevel(
        decimal value)
    {
        if (value >= 90)
        {
            return Result(
                RiskLevel.Red,
                "Nivel del río en condición crítica."
            );
        }

        if (value >= 75)
        {
            return Result(
                RiskLevel.Orange,
                "Nivel del río elevado."
            );
        }

        if (value >= 60)
        {
            return Result(
                RiskLevel.Yellow,
                "Nivel del río en observación."
            );
        }

        return Result(
            RiskLevel.Green,
            "Nivel del río dentro del rango seguro."
        );
    }


    private static RiskEvaluationResult Result(
        RiskLevel level,
        string message)
    {
        return new RiskEvaluationResult
        {
            Level = level,
            Message = message
        };
    }
}