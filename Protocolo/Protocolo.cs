// *****************************************************************************
// Práctica 07 - Protocolo
// Carlos Benavides
// Fecha de realización: 27/12/2024
// Fecha de entrega: 04/12/2024
//
// Resultados:
// * El protocolo define las estructuras de comunicación entre cliente y servidor mediante
//   las clases Pedido y Respuesta.
// * Proporciona métodos para procesar solicitudes del cliente y enviar respuestas del servidor.
//
// Conclusión:
// * En conclusión, la implementación de las clases Pedido y Respuesta en Protocolo con los métodos
//   correspondientes permite estructurar la comunicación entre cliente y servidor de mejor manera,
//   definiendo un formato claro para las solicitudes y respuestas. Esto permitirá agregar nuevos métodos
//   y funcionalidades sin afectar la estructura existente.
//
// Recomendaciones:
// * Validar más a fondo los datos recibidos en las solicitudes, como formatos y valores esperados, 
//   para garantizar que los comandos sean procesados correctamente y reducir errores.
// * Mejorar el manejo de excepciones en el protocolo para capturar y reportar fallos específicos 
//   durante la comunicación, facilitando el diagnóstico de problemas.
// *****************************************************************************

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Collections.Generic;

namespace Protocolo
{
    // Clase que representa un Pedido, enviado desde el cliente al servidor
    public class Pedido
    {
        public string Comando { get; set; } // Comando principal (e.g., INGRESO, CALCULO, CONTADOR)
        public string[] Parametros { get; set; } // Parámetros asociados al comando

        // Procesa un mensaje recibido y lo convierte en un objeto Pedido
        public static Pedido Procesar(string mensaje)
        {
            var partes = mensaje.Split(' ');
            return new Pedido
            {
                Comando = partes[0].ToUpper(),
                Parametros = partes.Skip(1).ToArray()
            };
        }

        // Devuelve el Pedido como una cadena para ser enviado
        public override string ToString()
        {
            return $"{Comando} {string.Join(" ", Parametros)}";
        }
    }

    // Clase que representa una Respuesta, enviada desde el servidor al cliente
    public class Respuesta
    {
        public string Estado { get; set; } // Estado de la operación (e.g., OK, NOK)
        public string Mensaje { get; set; } // Mensaje descriptivo del resultado

        // Devuelve la Respuesta como una cadena para ser enviada
        public override string ToString()
        {
            return $"{Estado} {Mensaje}";
        }
    }

    // Clase principal que implementa el Protocolo de comunicación
    public class Protocolos
    {
        private NetworkStream flujo; // Flujo de datos para la comunicación TCP

        // Constructor: inicializa el flujo
        public Protocolos(NetworkStream flujo)
        {
            this.flujo = flujo;
        }

        // Método para realizar una operación enviando un Pedido y recibiendo una Respuesta
        public Respuesta HazOperacion(string comando, string[] parametros)
        {
            if (flujo == null)
                throw new InvalidOperationException("No hay conexión establecida.");

            try
            {
                // Crear y enviar el Pedido al servidor
                var pedido = new Pedido { Comando = comando, Parametros = parametros };
                byte[] bufferTx = Encoding.UTF8.GetBytes(pedido.ToString());
                flujo.Write(bufferTx, 0, bufferTx.Length);

                // Recibir la Respuesta del servidor
                byte[] bufferRx = new byte[1024];
                int bytesRx = flujo.Read(bufferRx, 0, bufferRx.Length);
                string mensaje = Encoding.UTF8.GetString(bufferRx, 0, bytesRx);

                // Procesar la respuesta recibida y retornarla como un objeto Respuesta
                var partes = mensaje.Split(' ');
                return new Respuesta
                {
                    Estado = partes[0],
                    Mensaje = string.Join(" ", partes.Skip(1).ToArray())
                };
            }
            catch (SocketException ex)
            {
                throw new InvalidOperationException($"Error al intentar transmitir: {ex.Message}", ex);
            }
        }

        // Método estático para procesar un Pedido en el servidor y devolver una Respuesta
        public static Respuesta ResolverPedido(string mensaje, string direccionCliente, ref Dictionary<string, int> listadoClientes)
        {
            Pedido pedido = Pedido.Procesar(mensaje); // Convierte el mensaje recibido en un Pedido
            Respuesta respuesta = new Respuesta { Estado = "NOK", Mensaje = "Comando no reconocido" };

            // Procesa el comando del Pedido
            switch (pedido.Comando)
            {
                case "INGRESO":
                    // Valida usuario y contraseña
                    if (pedido.Parametros.Length == 2 &&
                        pedido.Parametros[0] == "root" &&
                        pedido.Parametros[1] == "admin20")
                    {
                        respuesta = new Random().Next(2) == 0
                            ? new Respuesta { Estado = "OK", Mensaje = "ACCESO_CONCEDIDO" }
                            : new Respuesta { Estado = "NOK", Mensaje = "ACCESO_NEGADO" };
                    }
                    else
                    {
                        respuesta.Mensaje = "ACCESO_NEGADO";
                    }
                    break;

                case "CALCULO":
                    // Procesa información de la placa
                    if (pedido.Parametros.Length == 3)
                    {
                        string placa = pedido.Parametros[2];
                        if (Regex.IsMatch(placa, @"^[A-Z]{3}[0-9]{4}$"))
                        {
                            byte indicadorDia = ObtenerIndicadorDia(placa);
                            respuesta = new Respuesta
                            { Estado = "OK", Mensaje = $"{placa} {indicadorDia}" };

                            // Incrementa el contador de solicitudes del cliente
                            if (!listadoClientes.ContainsKey(direccionCliente))
                                listadoClientes[direccionCliente] = 0;

                            listadoClientes[direccionCliente]++;
                        }
                        else
                        {
                            respuesta.Mensaje = "Placa no válida";
                        }
                    }
                    break;

                case "CONTADOR":
                    // Devuelve el número de solicitudes realizadas por el cliente
                    respuesta = listadoClientes.ContainsKey(direccionCliente)
                        ? new Respuesta
                        { Estado = "OK", Mensaje = listadoClientes[direccionCliente].ToString() }
                        : new Respuesta { Estado = "NOK", Mensaje = "No hay solicitudes previas" };
                    break;
            }

            return respuesta;
        }

        // Método para calcular el día permitido según el último dígito de la placa
        private static byte ObtenerIndicadorDia(string placa)
        {
            int ultimoDigito = int.Parse(placa.Substring(6, 1));
            switch (ultimoDigito)
            {
                case 1:
                case 2:
                    return 0b00100000; // Lunes
                case 3:
                case 4:
                    return 0b00010000; // Martes
                case 5:
                case 6:
                    return 0b00001000; // Miércoles
                case 7:
                case 8:
                    return 0b00000100; // Jueves
                case 9:
                case 0:
                    return 0b00000010; // Viernes
                default:
                    return 0;
            }
        }
    }
}
