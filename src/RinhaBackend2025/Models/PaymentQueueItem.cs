using System;
using System.Threading;
using System.Threading.Tasks;

namespace RinhaBackend2025.Models
{
    /// <summary>
    /// Representa um item na fila de pagamentos
    /// </summary>
    public class PaymentQueueItem
    {
        /// <summary>
        /// ID de correlação do pagamento
        /// </summary>
        public required string CorrelationId { get; set; }

        /// <summary>
        /// Valor do pagamento
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Data e hora de criação do item na fila
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Data e hora da requisição
        /// </summary>
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Número de tentativas de processamento
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Token de cancelamento para operações assíncronas
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Indica se o item deve ser processado pelo fallback
        /// </summary>
        public bool UseFallback { get; set; }

        /// <summary>
        /// Task de conclusão do processamento
        /// </summary>
        public TaskCompletionSource<PaymentResponse> Completion { get; set; } = new();
    }
}
