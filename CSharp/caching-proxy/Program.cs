using Cocona;

CoconaApp.Run(async (int port, string origin) =>
{
  try
  {
    var proxy = new Proxy(port, origin);
    await proxy.Start();
  }
  catch (Exception)
  {
    Console.WriteLine("An error occured. Goodbye");
  }
});