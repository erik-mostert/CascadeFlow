namespace Cascade.Core.Enums;

public enum MessageIntent
{
  Unknown = 0,
  Send = 1,      // Command sent to specific endpoint
  Publish = 2,   // Event published to subscribers
  Reply = 3,     // Reply to a previous message
}