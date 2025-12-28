using System;

using OmniRAG.Core.Constants;

namespace OmniRAG.Infrastructure.Embeddings;

/// <summary>
/// Simplified BERT tokenizer for sentence-transformers models.
/// This is a basic implementation - for production, consider using a full tokenizer library.
/// </summary>
internal class BertTokenizer
{
    private readonly int maxTokens;
    private const int ClsTokenId = DefaultValues.ClsTokenId;  // [CLS] token
    private const int SepTokenId = DefaultValues.SepTokenId;  // [SEP] token
    private const int PadTokenId = DefaultValues.PadTokenId;    // [PAD] token

    public BertTokenizer(string tokenizerPath, int maxTokens)
    {
        this.maxTokens = maxTokens;
        // Note: For full implementation, load vocabulary from tokenizer.json
        // This is a simplified version for demonstration
    }

    public (long[] tokens, long[] attentionMask, long[] tokenTypeIds) Tokenize(string text)
    {
        // Simplified tokenization - in production, use proper BPE/WordPiece tokenization
        // For now, use character-level tokenization as placeholder
        long[] tokens = new long[this.maxTokens];
        long[] attentionMask = new long[this.maxTokens];
        long[] tokenTypeIds = new long[this.maxTokens]; // All zeros for single sentence

        // Add [CLS] token
        tokens[0] = ClsTokenId;
        attentionMask[0] = 1;
        tokenTypeIds[0] = 0;

        // Simple character encoding (placeholder - use proper tokenizer in production)
        int position = 1;
        foreach (char c in text)
        {
            if (position >= this.maxTokens - 1)
            {
                break;
            }

            tokens[position] = (long)Math.Clamp((int)c, 0, DefaultValues.MaxCharacterTokenValue);
            attentionMask[position] = 1;
            tokenTypeIds[position] = 0; // First sentence segment
            position++;
        }

        // Add [SEP] token
        if (position < this.maxTokens)
        {
            tokens[position] = SepTokenId;
            attentionMask[position] = 1;
            tokenTypeIds[position] = 0;
            position++;
        }

        // Pad remaining positions
        for (int i = position; i < this.maxTokens; i++)
        {
            tokens[i] = PadTokenId;
            attentionMask[i] = 0;
            tokenTypeIds[i] = 0;
        }

        return (tokens, attentionMask, tokenTypeIds);
    }
}
