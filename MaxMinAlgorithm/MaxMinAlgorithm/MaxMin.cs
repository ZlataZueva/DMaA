using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMinAlgorithm
{
    class MaxMin<TObject> where TObject:struct
    {
        static public readonly int MaxObjectsAmount = 100000;
        static public readonly int MinObjectsAmount = 1000;

        int _objectsAmount;
        int _classesAmount;

        public int ObjectsAmount
        {
            get
            {
                return _objectsAmount;
            }
            private set
            {
                if (value >= MinObjectsAmount && value <= MaxObjectsAmount)
                {
                    _objectsAmount = value;
                }
            }
        }

        public int ClassesAmount
        {
            get
            {
                return _classesAmount;
            }
            private set
            {
                _classesAmount = value;
            }
        }

        public MaxMin(int objectsAmount)
        {
            this.ObjectsAmount = objectsAmount;
        }

        public List<int> ChooseTwoKernels (TObject[] objects, Func<TObject, TObject, double> Distance)
        {
            List<int> kernels = new List<int>();
            if (objects.Count() == _objectsAmount)
            {
                Random random = new Random();
                kernels.Add(random.Next(objects.Count()));
                double maxDistance = 0;
                int secondKernelIndex = 0;
                for (int i=0; i<objects.Count();i++)
                {
                    double distance = Distance(objects[i], objects[kernels[0]]);
                    if (distance>maxDistance)
                    {
                        maxDistance = distance;
                        secondKernelIndex = i;
                    }
                }
                kernels.Add(secondKernelIndex);
            }
            return kernels;
        }

        public Dictionary<TObject, int> DivideIntoClasses(TObject[] objects, List<int> kernelsIndexes, Func<TObject, TObject, double> Distance)
        {
            Dictionary<TObject, int> division = new Dictionary<TObject, int>(objects.Count());
            if (objects.Count() == _objectsAmount)
            {
                Task<int>[] tasksArr = new Task<int>[_objectsAmount];
                for (int i = 0; i < _objectsAmount; i++)
                {
                    tasksArr[i] = new Task<int>((objNum) =>
                    {
                        double minDistance = double.MaxValue;
                        int classNum = 0;
                        for (int j = 0; j < kernelsIndexes.Count(); j++)
                        {
                            double distance = Distance(objects[(int)objNum], objects[kernelsIndexes[j]]);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                                classNum = j;
                            }
                        }
                        return classNum;
                    }, i);
                    tasksArr[i].Start();
                }
                for (int i = 0; i < _objectsAmount; i++)
                {
                    tasksArr[i].Wait();
                    division[objects[i]] = tasksArr[i].Result;
                }
            }
            return division;
        }

        private TObject FindClassMostRemote(List<TObject> classObjects, TObject kernel, Func<TObject, TObject, double> Distance)
        {
            TObject theMostRemote = new TObject();
            if (classObjects.Count() > 0)
            {
                double maxDistance = 0;
                for (int objectNum = 0; objectNum < classObjects.Count(); objectNum++)
                {
                    double distance = Distance(kernel, classObjects[objectNum]);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        theMostRemote = classObjects[objectNum];
                    }
                } 
            }
            return theMostRemote;
        }

        public bool TryFindNewKernel (Dictionary<TObject, int> classesDictionary, TObject[] objects, ref List<int> kernelsIndexes, Func<TObject, TObject, double> Distance)
        {
            bool isFound = false;
            if (objects.Count() == _objectsAmount)
            {
                _classesAmount = kernelsIndexes.Count();
                Task<TObject>[] tasksArr = new Task<TObject>[_classesAmount];
                List<TObject> kernels = new List<TObject>();
                for (int classNum = 0; classNum < _classesAmount; classNum++)
                {
                    kernels.Add(objects[kernelsIndexes[classNum]]);
                }

                for (int classNum = 0; classNum < _classesAmount; classNum++)
                {
                    List<TObject> classObjects = new List<TObject>();
                    for (int objectNum = 0; objectNum < _objectsAmount; objectNum++)
                    {
                        if (classesDictionary[objects[objectNum]] == classNum)
                        {
                            classObjects.Add(objects[objectNum]);
                        }
                    }
                    tasksArr[classNum] = new Task<TObject>((obj) => { return FindClassMostRemote(classObjects, kernels[(int)obj], Distance); },classNum);
                    tasksArr[classNum].Start();
                }
                TObject theMostRemote = new TObject();
                double maxDistance = 0;
                for (int classNum = 0; classNum < _classesAmount; classNum++)
                {
                    tasksArr[classNum].Wait();
                    TObject newKernel = tasksArr[classNum].Result;
                    double distance = Distance(newKernel, kernels[classNum]);
                    if (distance>maxDistance)
                    {
                        maxDistance = distance;
                        theMostRemote = newKernel;
                    }
                }
                
                if (maxDistance>(FindAverageDistance(kernels,Distance)/2))
                {
                    isFound = true;
                    _classesAmount++;
                    kernelsIndexes.Add(Array.IndexOf(objects, theMostRemote));
                }
            }
            return isFound;
        }

        private double FindAverageDistance (List<TObject> kernels, Func<TObject, TObject, double> Distance)
        {
            double distance = 0;
            int amount = 0;
            for (int i=0; i<kernels.Count();i++)
            {
                for (int j = i + 1; j < kernels.Count(); j++)
                {
                    distance += Distance(kernels[i], kernels[j]);
                    amount++;
                }
            }
            return distance / amount;
        }
    }
}
