namespace CloudCafe {
 public struct CafeUiState {public bool Dialogue,Order,Recipe,Steps,Hold,Finish,Serve,Retry;public bool CanFinish;public int ActiveStep;}
 public static class CafeUiRules {
  public static CafeUiState For(CafeState s){
   bool brew=s.Stage==Phase.Grind||s.Stage==Phase.Extract||s.Stage==Phase.Milk||s.Stage==Phase.Ready;
   return new CafeUiState{
    Dialogue=s.Stage==Phase.Dialogue||s.Stage==Phase.Order||s.Stage==Phase.Reaction||s.Stage==Phase.Done,
    Order=s.Stage==Phase.Order,Recipe=s.Stage==Phase.Order||brew,Steps=brew,
    Hold=s.Stage==Phase.Grind||s.Stage==Phase.Extract||s.Stage==Phase.Milk,
    Finish=s.Stage==Phase.Milk,CanFinish=s.Milk+s.Spill>0,Serve=s.Stage==Phase.Ready,Retry=brew,
    ActiveStep=s.Stage==Phase.Grind?0:s.Stage==Phase.Extract?1:s.Stage==Phase.Milk?2:3
   };
  }
 }
}
