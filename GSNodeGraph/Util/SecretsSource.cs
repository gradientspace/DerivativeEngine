// Copyright Gradientspace Corp. All Rights Reserved.
using System;

namespace Gradientspace.NodeGraph
{
    /// <summary>
    /// standard interface that is implemented externally and provided to SecretsSource
    /// to allow things like API keys to be retrieved by node internals.
    /// 
    /// A set of standard key names are defined here, for convenience...
    /// </summary>
    public interface ISecretsSource
    {
        const string ANTHROPIC_API_KEY = "ANTHROPIC_API_KEY";

        bool FindSecret(string SecretName, out string Secret);
    }


    /// <summary>
    /// Static access to external secrets for Nodes
    /// </summary>
    public static class SecretsSource
    {
        static ISecretsSource? CurrentSource = null;

        public static void InitializeSecretsSource(ISecretsSource Source)
        {
            CurrentSource = Source;
        }

        public static bool FindSecret(string SecretName, out string Secret)
        {
            Secret = "";
            if (CurrentSource == null)
                return false;
            return CurrentSource.FindSecret(SecretName, out Secret);
        }
    }
}
