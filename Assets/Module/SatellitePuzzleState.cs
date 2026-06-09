using System;
using System.Collections.Generic;
using System.Linq;

public class SatellitePuzzleState
{
    public SatelliteGravityGrid GravityGrid;

    public SatelliteVector PlayerPosition;

    public SatelliteVector PlayerVelocity;

    public int PlayerRotation;

    public int PlayerAngularMomentum;

    public int PlayerFuel;

    public int GameAge;

    public SatelliteVector Target;

    public int TargetResearchLeft;

    public List<SatelliteVector> Obstacles;

    public List<string> EntityDescriptions;

    public float DyingPoint;

    public SatellitePuzzleState(int fuel, SatelliteVector target, int targetResearch, SatelliteGravityGrid gravity, List<SatelliteVector> obstacles, List<string> entityDescriptions)
    {
        PlayerFuel = fuel;
        Target = target;
        TargetResearchLeft = targetResearch;
        GravityGrid = gravity;
        PlayerPosition = new SatelliteVector();
        PlayerVelocity = new SatelliteVector(1, 0, 0);
        PlayerRotation = 0;
        PlayerAngularMomentum = 0;
        GameAge = 0;
        Obstacles = obstacles;
        EntityDescriptions = entityDescriptions;
        DyingPoint = -1;
    }

    public SatellitePuzzleState(SatellitePuzzleState old)
    {
        GravityGrid = old.GravityGrid;
        PlayerPosition = old.PlayerPosition;
        PlayerVelocity = old.PlayerVelocity;
        PlayerRotation = old.PlayerRotation;
        PlayerAngularMomentum = old.PlayerAngularMomentum;
        PlayerFuel = old.PlayerFuel;
        GameAge = old.GameAge;
        Target = old.Target;
        TargetResearchLeft = old.TargetResearchLeft;
        Obstacles = old.Obstacles;
        EntityDescriptions = old.EntityDescriptions;
        DyingPoint = old.DyingPoint;
    }

    public SatellitePuzzleState GenerateContinuation(int type)
    {
        SatellitePuzzleState copy = new SatellitePuzzleState(this);
        switch (type)
        {
            case -1:
                copy.Iterate();
                return copy;
            case 0:
                copy.PlayerVelocity += SatelliteVector.RotationToVector(copy.PlayerRotation);
                copy.PlayerFuel--;
                copy.Iterate();
                return copy;
            case 1:
                copy.PlayerAngularMomentum--;
                copy.PlayerFuel--;
                copy.Iterate();
                return copy;
            case 2:
                copy.PlayerAngularMomentum++;
                copy.PlayerFuel--;
                copy.Iterate();
                return copy;
            default:
                return null;
        }
    }

    public void Iterate()
    {
        PlayerVelocity += GravityGrid.GetForce(PlayerPosition);
        SatelliteVector primitive = PlayerVelocity.Primitive();
        int steps = PlayerVelocity.RingLength() == 0 ? 0 : PlayerVelocity.RingLength() / primitive.RingLength();
        for (int i = 0; i < steps; i++)
        {
            //game doesn't care much about death here. needs to be handled externally.
            PlayerPosition += primitive;
            if (Obstacles.Any(x => x.Matches(PlayerPosition)))
            {
                DyingPoint = (float)(i + 1) / steps;
                break;
            }
        }
        
        PlayerRotation += PlayerAngularMomentum;
        GameAge++;
        if ((PlayerPosition + SatelliteVector.RotationToVector(PlayerRotation)).Matches(Target))
            TargetResearchLeft--;
    }

    public string GetData()
    {
        return PlayerPosition + "-" + PlayerVelocity + "-" + ((PlayerRotation % 6 + 6) % 6) + "-" + ((PlayerAngularMomentum % 6 + 6) % 6) + "-" + PlayerFuel + "-" + TargetResearchLeft;
    }
}
