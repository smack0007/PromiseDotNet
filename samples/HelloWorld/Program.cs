﻿using System;
using System.Threading;
using PromiseDotNet;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            var promise = new Promise<int>((resolve, reject) =>
            {
                Thread.Sleep(1000);

                if ((new Random()).Next() % 2 == 0)
                {
                    resolve(42);
                }
                else
                {
                    reject(new InvalidOperationException("You are rejected."));
                }
            })
            .Then(
                x => Console.WriteLine($"Resolved: {x}"),
                x => Console.WriteLine($"Rejected: {x}")
            );

            Console.WriteLine("Waiting on promise...");

            while (promise.State == PromiseState.Pending)
                Thread.Sleep(100);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
