using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public sealed class BrewModel {
  public sealed class Parcel {public float Age,Amount;}
  public readonly List<Parcel> Parcels=new List<Parcel>();
  public const float Target=36,Travel=.22f;
  public bool Inserted{get;private set;} public bool Locked{get{return Inserted&&Angle>=39.9f;}}
  public float Angle{get;private set;} public bool CupDocked,Running;
  public float Pumped{get;private set;}public float InFlight{get;private set;}public float InCup{get;private set;}public float Spilled{get;private set;}
  float elapsed;
  private const float ProfessionalCupLimit = 60f;
  private ProfessionalEspressoModel professional;
  public bool IsProfessional => professional != null;
  public bool ShotFinalized { get; private set; }
  public float ElapsedSeconds => elapsed;
  public bool Complete => !Running && (IsProfessional
      ? ShotFinalized && InCup > 0f && Parcels.Count == 0
      : InCup >= 35.999f && InFlight < .001f);

  public bool ConfigureProfessional(ProfessionalEspressoModel recipe)
  {
      if (Running || elapsed > 0f || Pumped > 0f || ShotFinalized) return false;
      professional = recipe;
      return true;
  }

  public bool Insert(bool prepared){if(!prepared||Inserted)return false;Inserted=true;return true;}
  public void Turn(float degrees){if(!Inserted||Running||Locked||float.IsNaN(degrees)||float.IsInfinity(degrees))return;Angle=Math.Max(0,Math.Min(40,Angle+degrees));}
  public bool TogglePump()
  {
      if (Running)
      {
          Running = false;
          if (IsProfessional)
          {
              ShotFinalized = Pumped > 0f;
              if (!ShotFinalized) elapsed = 0f;
          }
          return true;
      }
      if (!Locked || !CupDocked || (IsProfessional ? ShotFinalized : Pumped >= Target - .001f)) return false;
      Running = true;
      return true;
  }

  private void AddParcel(float amount)
  {
      if (amount <= 0f) return;
      Pumped += amount;
      InFlight += amount;
      Parcels.Add(new Parcel { Amount = amount });
  }

  private void StepProfessional(float dt)
  {
      float pumpingTime = Math.Min(dt, ProfessionalEspressoModel.ShotLimitSeconds - elapsed);
      float flow = professional.FlowGramsPerSecond(elapsed + pumpingTime * .5f);
      elapsed += pumpingTime;
      AddParcel(Math.Min(pumpingTime * flow, Math.Max(0f, ProfessionalCupLimit - Pumped)));
      if (elapsed < ProfessionalEspressoModel.ShotLimitSeconds && Pumped < ProfessionalCupLimit - .0001f)
          return;
      ShotFinalized = true;
      Running = false;
  }

  public void Step(float dt)
  {
      if (float.IsNaN(dt) || float.IsInfinity(dt) || dt <= 0f) return;
      dt = Math.Min(.05f, dt);
      for (int i = Parcels.Count - 1; i >= 0; i--)
      {
          Parcel parcel = Parcels[i];
          parcel.Age += dt;
          if (parcel.Age < Travel) continue;
          InFlight -= parcel.Amount;
          if (CupDocked) InCup += parcel.Amount;
          else Spilled += parcel.Amount;
          Parcels.RemoveAt(i);
      }
      if (Parcels.Count == 0) InFlight = 0f;

      if (!Running) return;
      if (!Locked || !CupDocked)
      {
          Running = false;
          if (IsProfessional && Pumped <= 0f) elapsed = 0f;
          return;
      }
      if (IsProfessional)
      {
          StepProfessional(dt);
          return;
      }
      elapsed += dt;
      if (elapsed < 1.2f) return;
      AddParcel(Math.Min(dt * 4.5f, Math.Max(0f, Target - Pumped)));
      if (Pumped >= Target - .0001f) Running = false;
  }
  public void Reset(){Inserted=CupDocked=Running=ShotFinalized=false;Angle=Pumped=InFlight=InCup=Spilled=elapsed=0;professional=null;Parcels.Clear();}
 }
}
