// *****************************************************************************
// Práctica 07 - Cliente
// Carlos Benavides
// Fecha de realización: 27/12/2024
// Fecha de entrega: 04/12/2024
//
// Resultados:
// * El cliente permite interactuar con el servidor para validar accesos,
//   consultar información de placas y obtener estadísticas de las solicitudes
//   realizadas.
// * La interfaz gráfica está diseñada para mostrar mensajes claros sobre los resultados
//   de las operaciones realizadas o para informar de algún error ocurrido de la solicitud.
//
// Conclusión:
// * En conclusión, la estructura del cliente basada en la clase Protocolo, simplifica el manejo de
//   solicitudes y respuestas, mejorando la modularidad del sistema y el entendimiento del código.
//
// Recomendaciones:
// * Implementar más validaciones en los campos ingresados por el usuario para evitar errores.
// * Considerar añadir mensajes más descriptivos en caso de error para mejorar la experiencia del usuario.
// *****************************************************************************


using System;
using System.Net.Sockets;
using System.Windows.Forms;
using Protocolo;

namespace Cliente
{
    public partial class FrmValidador : Form
    {
        // Variables para manejar la conexión al servidor
        private TcpClient remoto; // Cliente TCP para conectarse al servidor
        private NetworkStream flujo; // Flujo de datos entre cliente y servidor
        private Protocolos protocolo; // Instancia para manejar operaciones del protocolo

        public FrmValidador()
        {
            InitializeComponent();
        }

        private void FrmValidador_Load(object sender, EventArgs e)
        {
            try
            {
                // Establece la conexión al servidor en el puerto 5000
                remoto = new TcpClient("127.0.0.1", 5000);
                flujo = remoto.GetStream();
                protocolo = new Protocolos(flujo);

                // Desactiva la sección de consulta hasta que se valide el acceso
                panPlaca.Enabled = false;
            }
            catch (SocketException ex)
            {
                // Muestra un mensaje de error si la conexión falla
                MessageBox.Show("No se pudo establecer conexión: " + ex.Message, "ERROR");
            }
        }

        private void btnIniciar_Click(object sender, EventArgs e)
        {
            string usuario = txtUsuario.Text;
            string contraseña = txtPassword.Text;

            // Valida que los campos no estén vacíos
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contraseña))
            {
                MessageBox.Show("Se requiere el ingreso de usuario y contraseña", "ADVERTENCIA");
                return;
            }

            try
            {
                // Envía el comando de ingreso al servidor
                var respuesta = protocolo.HazOperacion("INGRESO", new[] { usuario, contraseña });

                if (respuesta.Estado == "OK" && respuesta.Mensaje == "ACCESO_CONCEDIDO")
                {
                    // Habilita la sección de consulta si el acceso es concedido
                    panPlaca.Enabled = true;
                    panLogin.Enabled = false;
                    txtModelo.Focus();

                    MessageBox.Show("Acceso concedido", "INFORMACIÓN");
                }
                else
                {
                    // Muestra un mensaje si el acceso es denegado
                    MessageBox.Show("No se pudo ingresar, revise credenciales", "ERROR");
                }
            }
            catch (Exception ex)
            {
                // Manejo de errores generales durante la operación
                MessageBox.Show("Error: " + ex.Message, "ERROR");
            }
        }

        private void btnConsultar_Click(object sender, EventArgs e)
        {
            try
            {
                // Envía el comando de cálculo al servidor
                var respuesta = protocolo.HazOperacion("CALCULO", new[] { txtModelo.Text, txtMarca.Text, txtPlaca.Text });

                // Muestra la respuesta del servidor
                MessageBox.Show("Respuesta recibida: " + respuesta.Mensaje, "INFORMACIÓN");
            }
            catch (Exception ex)
            {
                // Manejo de errores generales durante la operación
                MessageBox.Show("Error: " + ex.Message, "ERROR");
            }
        }

        private void btnNumConsultas_Click(object sender, EventArgs e)
        {
            try
            {
                // Envía el comando para obtener el número de consultas realizadas
                var respuesta = protocolo.HazOperacion("CONTADOR", new string[0]);

                // Muestra el número de consultas realizadas
                MessageBox.Show($"Número de consultas: {respuesta.Mensaje}", "INFORMACIÓN");
            }
            catch (Exception ex)
            {
                // Manejo de errores generales durante la operación
                MessageBox.Show("Error: " + ex.Message, "ERROR");
            }
        }

        private void FrmValidador_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Cierra el flujo y la conexión TCP al cerrar el formulario
            flujo?.Close();
            remoto?.Close();
        }
    }
}
