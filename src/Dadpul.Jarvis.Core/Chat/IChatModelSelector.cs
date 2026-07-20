using System;
using System.Collections.Generic;
using System.Text;

namespace Dadpul.Jarvis.Core.Chat
{
    
    public interface IChatModelSelector
    {
        Task<IChatModel> SelectAsync(CancellationToken cancellationToken);
    }
    public sealed class ChatModelSelector : IChatModelSelector
    {
        #region Constants and Fields

        
        private readonly IEnumerable<IChatModel> models;
        #endregion

        #region Constructors and Destructors

        public ChatModelSelector(IEnumerable<IChatModel> models)
        {
            this.models = models;
        }

        #endregion

        #region IChatModelSelector Members

        public async Task<IChatModel> SelectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var model in models) 
            {
                if (await model.IsAvailableAsync(cancellationToken))
                {
                    Console.WriteLine($"{model.Descriptor.Name} Available.");
                    return model;
                }
            }
            return null;
            
        }

        #endregion
    }
}
