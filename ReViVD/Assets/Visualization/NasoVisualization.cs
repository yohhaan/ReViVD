﻿using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Revivd {

    public class NasoVisualization : TimeVisualization {
        public List<NasoPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filenameBase = "";

        public int particlesStart = 0;
        public int n_particles = 1000;
        public int particlesStep = 1;
        public bool randomParticles = false;

        public int instantsStart = 0;
        public int n_instants = 50;
        public int instantsStep = 1;

        private int sizeCoeff = 50;

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }


        public Vector3 maxPoint = new Vector3(500, 500, 500);
        public Vector3 minPoint = new Vector3(-500, -500, -500);

        protected override bool LoadFromFile() {
            if (filenameBase == "")
                return false;

            int[] keptParticles = new int[n_particles];
            Color32[] pathColors = new Color32[n_particles];

            Tools.StartClock();

            if (randomParticles) {
                SortedSet<int> chosenRandomParticles = new SortedSet<int>();
                System.Random rnd = new System.Random();
                for (int i = 0; i < n_particles; i++) {
                    while (!chosenRandomParticles.Add(rnd.Next(1000000))) { }
                }
                chosenRandomParticles.CopyTo(keptParticles);
            }
            else {
                for (int i = 0; i < n_particles; i++)
                    keptParticles[i] = particlesStart + particlesStep * i;
            }

            Tools.AddClockStop("generated particles array");

            paths = new List<NasoPath>(n_particles);
            for (int i = 0; i < n_particles; i++) {
                GameObject go = new GameObject(keptParticles[i].ToString());
                go.transform.parent = transform;
                paths.Add(go.AddComponent<NasoPath>());
                pathColors[i] = Random.ColorHSV();
            }

            Tools.AddClockStop("created paths");

            int currentFileNumber = instantsStart / 50 + 1;
            int instant = instantsStart;
            int localInstant = instantsStart % 50;
            BinaryReader br = new BinaryReader(File.Open(filenameBase + currentFileNumber.ToString("0000") + ".bytes", FileMode.Open));
            Tools.AddClockStop("Loaded file " + currentFileNumber.ToString("0000"));
            for (int t = 0; t < n_instants; t++) {
                if (localInstant >=  50) {
                    Tools.AddClockStop("Finished loading from file " + currentFileNumber.ToString("0000"));

                    localInstant %= 50;
                    br.Dispose();
                    Tools.AddClockStop("Closed file " + currentFileNumber.ToString("0000"));

                    currentFileNumber++;
                    br = new BinaryReader(File.Open(filenameBase + currentFileNumber.ToString("0000") + ".bytes", FileMode.Open));
                    Tools.AddSubClockStop("BinaryReader creation");

                    Tools.AddClockStop("Loaded file " + currentFileNumber.ToString("0000"));
                }

                for (int i = 0; i < n_particles; i++) {
                    Vector3 point = new Vector3();
                    br.BaseStream.Position = (3 * 1000000 * localInstant + keptParticles[i]) * 8;
                    point.x = ((float)br.ReadDouble() - 317) * sizeCoeff;
                    br.BaseStream.Position += 1000000 * 8 - 8; //Going back 8 bytes because reading the data advances the position
                    point.z = ((float)br.ReadDouble() - 317) * sizeCoeff;
                    br.BaseStream.Position += 1000000 * 8 - 8;
                    point.y = ((float)br.ReadDouble() - 317) * sizeCoeff;

                    point = Vector3.Max(point, minPoint);
                    point = Vector3.Min(point, maxPoint);

                    NasoAtom a = new NasoAtom() {
                        time = (float)instant,
                        point = point,
                        path = paths[i],
                        indexInPath = t
                    };

                    a.BaseColor = pathColors[i];
                    paths[i].atoms.Add(a);
                }

                Tools.AddSubClockStop("Loaded particles for instant " + instant);

                instant += instantsStep;
                localInstant += instantsStep;
            }

            Tools.EndClock();

            return true;
        }

        protected override float InterpretTime(string word) {
            return 0;
        }

        public class NasoPath : TimePath {
            public List<NasoAtom> atoms = new List<NasoAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class NasoAtom : TimeAtom {
        }
    }

}