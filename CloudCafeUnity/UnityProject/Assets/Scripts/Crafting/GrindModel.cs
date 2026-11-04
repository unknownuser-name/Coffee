using System;
using System.Collections.Generic;
namespace CloudCafe.Crafting {
 public enum CraftSlot {Invalid,Mat,Grinder,Held,Machine}
 public sealed class GrindModel {
  public sealed class Parcel {public float Age,Amount;}
  public readonly List<Parcel> Parcels=new List<Parcel>();
  public const float Travel=.22f;
  public float Beans=60,Flying,Grounds,Waste;
  public CraftSlot Slot=CraftSlot.Mat;
  CraftSlot origin=CraftSlot.Mat;
  public bool Running;
  public bool ManualDose { get; private set; }
  public bool DoseCommitted { get; private set; }
  public float DoseInBasketOrFlight => Grounds + Flying;
  private float DoseLimit => ManualDose ? ProfessionalEspressoModel.MaxDoseGrams : 18f;
  public bool Ready => Flying < .001f && (ManualDose
      ? !Running && Grounds >= ProfessionalEspressoModel.MinDoseGrams - .001f
      : Grounds >= 17.99f);
  public bool Complete => Ready && Slot == CraftSlot.Mat && (!ManualDose || DoseCommitted);

  public bool SetManualDose(bool enabled)
  {
      if (Running || Grounds > 0f || Flying > 0f || DoseCommitted) return false;
      ManualDose = enabled;
      return true;
  }

  public bool CommitDose()
  {
      if (!ManualDose || DoseCommitted || Slot != CraftSlot.Mat || !Ready) return false;
      DoseCommitted = true;
      return true;
  }
  public bool PickUp(){if(Slot==CraftSlot.Held)return false;if(Running){Running=false;return false;}if(Flying>.001f)return false;origin=Slot;Slot=CraftSlot.Held;return true;}
  public void Drop(CraftSlot slot){if(Slot!=CraftSlot.Held)return;Slot=slot==CraftSlot.Grinder||slot==CraftSlot.Mat||slot==CraftSlot.Machine?slot:origin;}
  public bool Toggle()
  {
      if (Running)
      {
          Running = false;
          return true;
      }
      if (DoseCommitted || Slot != CraftSlot.Grinder || Beans <= 0f || DoseInBasketOrFlight >= DoseLimit - .01f)
          return false;
      Running = true;
      return true;
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
          Flying -= parcel.Amount;
          if (Slot == CraftSlot.Grinder) Grounds += parcel.Amount;
          else Waste += parcel.Amount;
          Parcels.RemoveAt(i);
      }
      if (Parcels.Count == 0) Flying = 0f;

      if (!Running) return;
      if (Slot != CraftSlot.Grinder)
      {
          Running = false;
          return;
      }
      float amount = Math.Min(Beans, Math.Min(dt * 4.5f, Math.Max(0f, DoseLimit - Grounds - Flying)));
      Beans -= amount;
      Flying += amount;
      if (amount > 0f) Parcels.Add(new Parcel { Amount = amount });
      if (Beans <= .0001f || DoseInBasketOrFlight >= DoseLimit - .001f) Running = false;
  }
  public void Reset(){Beans=60;Flying=Grounds=Waste=0;Running=DoseCommitted=false;Slot=origin=CraftSlot.Mat;Parcels.Clear();}
 }
}
