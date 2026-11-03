namespace CloudCafe.Crafting {
 public enum HandPose {Open,Hover,Grip,Press}
 public static class HandCursorModel {
  public static HandPose Resolve(bool held,bool pressing,bool hover,bool paused){return paused?HandPose.Open:pressing?HandPose.Press:held?HandPose.Grip:hover?HandPose.Hover:HandPose.Open;}
 }
}
