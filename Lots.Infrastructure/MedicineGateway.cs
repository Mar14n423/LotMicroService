using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http; 
namespace Lots.Infrastructure.Gateways
{
    public class MedicineGateway
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public MedicineGateway(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> ExistsMedicine(int medicineId)
        {
            var client = _httpClientFactory.CreateClient("MedicinesApi"); // Asegúrate de registrar esto en Program.cs
                                                                          // No necesitamos enviar token si es una comunicación interna confiable,
                                                                          // pero si la API de Medicines tiene [Authorize], necesitarás pasar el token.
                                                                          // Para simplificar ahora, asumimos que Medicines tiene un endpoint público o usamos un token de servicio.

            var response = await client.GetAsync($"api/medicines/{medicineId}");
            return response.IsSuccessStatusCode;
        }
    }
}