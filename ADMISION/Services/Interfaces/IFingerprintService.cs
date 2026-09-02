using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADMISION.Services.Interfaces
{
    public interface IFingerprintService
    {
        Task<bool> ConnectAsync(string ipAddress, int port = 4370);
        Task<bool> CheckConnectionAsync(string ipAddress, int port = 4370);
        Task DisconnectAsync();
        Task<string?> CaptureTemplateAsync(); // Captures a template from the device
        Task<bool> RegisterFingerprintAsync(Guid? postulantId, int fingerIndex, string template);
        Task<(bool Success, Guid? PostulantId)> ValidateFingerprintAsync(string template);
    }
}
