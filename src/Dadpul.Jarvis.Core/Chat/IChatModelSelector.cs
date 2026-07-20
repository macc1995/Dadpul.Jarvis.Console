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

        private readonly IChatModel primaryModel;

        #endregion

        #region Constructors and Destructors

        public ChatModelSelector(IChatModel primaryModel)
        {
            this.primaryModel = primaryModel;
        }

        #endregion

        #region IChatModelSelector Members

        public Task<IChatModel> SelectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(primaryModel);
        }

        #endregion
    }
}
