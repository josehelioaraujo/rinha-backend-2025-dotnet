using System.ComponentModel.DataAnnotations;

namespace RinhaBackend2025.Models;

public record PaymentRequest(
    [Required] string CorrelationId,
    [Required] decimal Amount
);

