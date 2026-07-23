// Made by Dadpul

namespace Dadpul.Jarvis.Core.Application.Propmpts;

public class CustomSystemPrompt : ISystemPrompt
{
   #region Constants and Fields

   private readonly string prompt;

   #endregion

   #region Constructors and Destructors

   public CustomSystemPrompt(string prompt)
   {
      this.prompt = prompt;
   }

   #endregion

   #region ISystemPrompt Members

   public string Content => prompt;

   #endregion
}