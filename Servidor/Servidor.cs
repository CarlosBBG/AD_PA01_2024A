// *****************************************************************************
// Práctica 07 - Servidor
// Carlos Benavides
// Fecha de realización: 27/12/2024
// Fecha de entrega: 04/12/2024
//
// Resultados:
// * El servidor es capaz de recibir solicitudes desde múltiples clientes y procesarlas 
//   utilizando el protocolo definido en la clase Protocolos, garantizando consistencia 
//   en la comunicación.
// * Maneja las conexiones de clientes de forma concurrente mediante hilos, lo que permite 
//   atender múltiples solicitudes de manera simultánea.
//
// Conclusioón:
// * La separación de lógica mediante el protocolo centraliza el manejo de solicitudes, 
//   permitiendo que el servidor sea fácilmente extensible para incorporar nuevas funcionalidades 
//   sin afectar la estructura existente.
//
// Recomendaciones:
// * Mejorar la gestión de errores para garantizar que los recursos como los flujos de datos y 
//   las conexiones sean liberados correctamente en caso de fallos.
// * Agregar más detalles en los logs, como información sobre el cliente (IP, tipo de error), 
//   para facilitar el monitoreo y la resolución de problemas.
// *****************************************************************************


using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Protocolo;

namespace Servidor
{
    class Servidor
    {
        private static TcpListener escuchador; // Escucha conexiones entrantes en el puerto 8080
        private static Dictionary<string, int> listadoClientes = new Dictionary<string, int>(); // Contador de operaciones por cliente

        static void Main(string[] args)
        {
            try
            {
                // Inicia el servidor para escuchar en el puerto 5000
                escuchador = new TcpListener(IPAddress.Any, 5000);
                escuchador.Start();
                Console.WriteLine("Servidor iniciado en el puerto 5000...");

                while (true)
                {
                    // Acepta conexiones de clientes
                    TcpClient cliente = escuchador.AcceptTcpClient();
                    Console.WriteLine("Cliente conectado desde: " + cliente.Client.RemoteEndPoint);

                    // Crea un hilo separado para manejar al cliente
                    Thread hiloCliente = new Thread(ManipuladorCliente);
                    hiloCliente.Start(cliente);
                }
            }
            catch (SocketException ex)
            {
                // Manejo de errores de conexión
                Console.WriteLine("Error de socket: " + ex.Message);
            }
            finally
            {
                // Cierra el servidor al finalizar
                escuchador?.Stop();
            }
        }

        private static void ManipuladorCliente(object obj)
        {
            TcpClient cliente = (TcpClient)obj; // Cliente conectado
            NetworkStream flujo = null; // Flujo de datos para la comunicación con el cliente

            try
            {
                flujo = cliente.GetStream(); // Obtiene el flujo de datos del cliente
                byte[] bufferRx = new byte[1024]; // Buffer para recibir datos
                int bytesRx;

                while ((bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length)) > 0)
                {
                    // Lee el mensaje enviado por el cliente
                    string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);
                    string direccionCliente = cliente.Client.RemoteEndPoint.ToString();

                    // Procesa el mensaje usando el protocolo y genera una respuesta
                    Respuesta respuesta = Protocolos.ResolverPedido(mensaje, direccionCliente, ref listadoClientes);
                    Console.WriteLine($"Pedido: {mensaje} | Respuesta: {respuesta}");

                    // Envía la respuesta de vuelta al cliente
                    byte[] bufferTx = Encoding.UTF8.GetBytes(respuesta.ToString());
                    flujo.Write(bufferTx, 0, bufferTx.Length);
                }
            }
            catch (SocketException ex)
            {
                // Manejo de errores durante la comunicación con el cliente
                Console.WriteLine("Error de cliente: " + ex.Message);
            }
            finally
            {
                // Cierra el flujo y la conexión del cliente
                flujo?.Close();
                cliente?.Close();
            }
        }
    }
}
