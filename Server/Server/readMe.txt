(TcpClient.Client的类型为System.Net.Sockets.Socket)

1. TcpClient.Connected: 属性获取截止到最后一次 I/O 操作时的 Client 套接字的连接状态。
2. TcpClient.Client.Connected: 属性获取截止到最后的 I/O 操作时 Socket 的连接状态。Connected 属性的值反映最近操作时的连接状态。 如果您需要确定连接的当前状态，请进行非阻止、零字节的 Send 调用。 如果该调用成功返回或引发 WAEWOULDBLOCK 错误代码 (10035)，则该套接字仍然处于连接状态；否则，该套接字不再处于连接状态。（MSDN上还有一段代码）
3. TcpClient.Available: 如果远程主机处于关机状态或关闭了连接，Available 可能会引发 SocketException。
4. TcpClient.Client.Available: 如果远程主机处于关机状态或关闭了连接，则 Available 会引发 SocketException。
5. Socket.Poll:  http://msdn.microsoft.com/zh-cn/library/vstudio/system.net.sockets.socket.poll(v=vs.100).aspx (主要看Mode为SelectRead时Poll的返回值)