﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using Microsoft.Crm.Sdk.Messages;

namespace McTools.Xrm.Connection.WinForms
{
    public class FormHelper
    {
        public FormHelper(Form innerAppForm, ConnectionManager connectionManager)
        {
            _innerAppForm = innerAppForm;
            _connectionManager = connectionManager;
        }

        private readonly Form _innerAppForm;
        private readonly ConnectionManager _connectionManager;

        /// <summary>
        /// Checks the existence of a user password and returns it
        /// </summary>
        /// <param name="detail">Details of the Crm connection</param>
        /// <returns>True if password defined</returns>
        public bool RequestPassword(ConnectionDetail detail)
        {
            if (!detail.PasswordIsEmpty)
                return true;

            bool returnValue = false;

            var pForm = new PasswordForm
            {
                UserLogin = detail.UserName,
                UserDomain = detail.UserDomain,
                StartPosition = FormStartPosition.CenterParent
            };

            MethodInvoker mi = delegate
            {
                if (pForm.ShowDialog(_innerAppForm) == DialogResult.OK)
                {
                    detail.SetPassword(pForm.UserPassword);
                    detail.SavePassword = pForm.SavePassword;
                    returnValue = true;
                }
            };

            if (_innerAppForm.InvokeRequired)
            {
                _innerAppForm.Invoke(mi);
            }
            else
            {
                mi();
            }

            return returnValue;
        }

        /// <summary>
        /// Asks this manager to select a Crm connection to use
        /// </summary>
        public bool AskForConnection(object connectionParameter)
        {
            var cs = new ConnectionSelector(_connectionManager.ConnectionsList, _connectionManager)
            {
                StartPosition = FormStartPosition.CenterParent,
            };

            if (cs.ShowDialog(_innerAppForm) == DialogResult.OK)
            {
                var connectionDetail = cs.SelectedConnections.First();
                if (connectionDetail.IsCustomAuth)
                {
                    if (connectionDetail.PasswordIsEmpty)
                    {
                        var pForm = new PasswordForm()
                        {
                            UserDomain = connectionDetail.UserDomain,
                            UserLogin = connectionDetail.UserName
                        };
                        if (pForm.ShowDialog(_innerAppForm) == DialogResult.OK)
                        {
                            connectionDetail.SetPassword(pForm.UserPassword);
                            connectionDetail.SavePassword = pForm.SavePassword;
                        }
                        else
                        {
                            return false;
                        }
                    }
                 }

                _connectionManager.ConnectToServer(connectionDetail, connectionParameter);

                if (cs.HadCreatedNewConnection)
                {
                    if (!_connectionManager.ConnectionsList.Connections.Contains(connectionDetail))
                        _connectionManager.ConnectionsList.Connections.Add(connectionDetail);
                    _connectionManager.SaveConnectionsFile(_connectionManager.ConnectionsList);
                }

                return true;
            }

            return false;
        }

        public void DisplayConnectionsList(Form form)
        {
            var cs = new ConnectionSelector(_connectionManager.ConnectionsList, _connectionManager, true, false)
            {
                StartPosition = FormStartPosition.CenterParent,
            };

            cs.ShowDialog(form);
        }

        public List<ConnectionDetail> SelectMultipleConnectionDetails()
        {
            var cs = new ConnectionSelector(_connectionManager.ConnectionsList, _connectionManager, true)
            {
                StartPosition = FormStartPosition.CenterParent,
            };

            if (cs.ShowDialog(_innerAppForm) == DialogResult.OK)
            {
                return cs.SelectedConnections;
            }

            return new List<ConnectionDetail>();
        }

        /// <summary>
        /// Creates or updates a Crm connection
        /// </summary>
        /// <param name="isCreation">Indicates if it is a connection creation</param>
        /// <param name="connectionToUpdate">Details of the connection to update</param>
        /// <returns>Created or updated connection</returns>
        public ConnectionDetail EditConnection(bool isCreation, ConnectionDetail connectionToUpdate)
        {
            var cForm = new ConnectionForm(isCreation) { StartPosition = FormStartPosition.CenterParent };

            if (!isCreation)
            {
                cForm.CrmConnectionDetail = connectionToUpdate;
            }

            if (cForm.ShowDialog(_innerAppForm) == DialogResult.OK)
            {
               
                if (cForm.DoConnect)
                {
                    _connectionManager.ConnectToServer(cForm.CrmConnectionDetail);
                }

                if (!cForm.CrmConnectionDetail.PasswordIsEmpty && !cForm.CrmConnectionDetail.SavePassword)
                {
                    cForm.CrmConnectionDetail.ErasePassword();
                }

                if (isCreation)
                {
                    if (!_connectionManager.ConnectionsList.Connections.Contains(cForm.CrmConnectionDetail))
                        _connectionManager.ConnectionsList.Connections.Add(cForm.CrmConnectionDetail);
                }
                else
                {
                    foreach (ConnectionDetail detail in _connectionManager.ConnectionsList.Connections)
                    {
                        if (detail.ConnectionId == cForm.CrmConnectionDetail.ConnectionId)
                        {
                            detail.UpdateAfterEdit(cForm.CrmConnectionDetail);
                        }
                    }
                }

                _connectionManager.SaveConnectionsFile(_connectionManager.ConnectionsList);

                return cForm.CrmConnectionDetail;
            }

            return null;
        }

        public IWebProxy SelectProxy()
        {
            if (_connectionManager.ConnectionsList.UseCustomProxy)
            {
                var proxy = new WebProxy(_connectionManager.ConnectionsList.ProxyAddress + ":" + _connectionManager.ConnectionsList.ProxyPort, true)
                {
                    Credentials =
                        new NetworkCredential(_connectionManager.ConnectionsList.UserName, _connectionManager.ConnectionsList.Password)
                };

                return proxy;
            }

            return null;
        }

        /// <summary>
        /// Deletes a Crm connection from the connections list
        /// </summary>
        /// <param name="connectionToDelete">Details of the connection to delete</param>
        public void DeleteConnection(ConnectionDetail connectionToDelete)
        {
            _connectionManager.ConnectionsList.Connections.Remove(connectionToDelete);
            _connectionManager.SaveConnectionsFile(_connectionManager.ConnectionsList);
        }
    }
}