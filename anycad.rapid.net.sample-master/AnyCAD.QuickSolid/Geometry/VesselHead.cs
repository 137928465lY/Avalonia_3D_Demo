using System.Collections.ObjectModel;
using AnyCAD.Foundation;
using AnyCAD.QuickSolid;
using System;

namespace AnyCAD.Demo.Geometry
{
    public class VesselHead 
    {        
        public string Name { get; set; } = "Default";
        #region Input Parameter
        public double Diameter { get; set; }

        public double Delta { get; set; }

        public double Height { get; set; }

        public HeadSetType HeadType { get; set; } = HeadSetType.EHB;
        #endregion
        public TopoShape InnerFace { get; set; }
        public TopoShape OuterFace { get; set; }
        #region Calculate Parameter
        /// <summary>
        /// 直边高度
        /// </summary>
        public double HeightFlat { get; set; }
        /// <summary>
        /// 容器外部直径
        /// </summary>
        public double DiameterOuter { get; set; }
        /// <summary>
        /// 容器内部直径
        /// </summary>
        public double DiameterInner { get; set; }
        /// <summary>
        /// 弧边外高
        /// </summary>
        public double HeightOuter { get; set; }
        /// <summary>
        /// 弧边内高
        /// </summary>
        public double HeightInner { get; set; }
        /// <summary>
        /// 碟型or球型内半径
        /// dished Head or spherical head 
        /// </summary>
        public double RadiusInner { get; set; }
        /// <summary>
        /// 碟型or球型外半径
        /// </summary>
        public double RadiusOuter { get; set; }
        /// <summary>
        /// 碟型连接圆弧内半径；折边平底封头折弯内半径
        /// </summary>
        public double RadiusJointInner { get; set; }
        /// <summary>
        /// 碟型连接圆弧外半径；折边平底封头折弯外半径
        /// </summary>
        public double RadiusJointOuter { get; set; }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="diameter"> 直径</param>
        /// <param name="delta">厚度</param>
        /// <param name="height">高度</param>
        /// <param name="type">封头类型,HeadSetType</param>
        public VesselHead(double diameter, double delta, double height, HeadSetType type,string name = "Default")
        {
            Diameter = diameter;
            Delta = delta;
            Height = height;
            HeadType = type;
            Name = name;
            switch (type)
            {
                case HeadSetType.EHA:
                    GetCalEHA();
                    break;
                case HeadSetType.EHB:
                    GetCalEHB();
                    break;
                case HeadSetType.THA:
                    GetCalTHA();
                    break;
                case HeadSetType.THB:
                    GetCalTHB();
                    break;
                case HeadSetType.FHA:
                    GetCalFHA();
                    break;
                case HeadSetType.DHA:
                    GetCalDHA();
                    break;
                case HeadSetType.HHA:
                    GetCalHHA();
                    break;
                case HeadSetType.MD:
                    GetCalMD();
                    break;
                default:
                    break;
            }
        }


        #region Method to Get the Calculate parameters
        void GetCalEHA()
        {
            HeightInner = Diameter/4 ;
            HeightOuter = HeightInner+Delta;
            HeightFlat = Height - HeightInner;
            DiameterInner = Diameter;
            DiameterOuter = Diameter + 2*Delta;
            ///Invalid parameter
            RadiusInner = 0;
            RadiusOuter = 0;
            RadiusJointInner = 0;
            RadiusJointOuter = 0;
        }
        void GetCalEHB()
        {
            HeightOuter = Diameter / 4;
            HeightInner = HeightOuter-Delta;
            HeightFlat = Height - HeightOuter;
            DiameterOuter = Diameter;
            DiameterInner = Diameter - 2*Delta;
            ///Invalid parameter
            RadiusInner = 0;
            RadiusOuter = 0;
            RadiusJointInner = 0;
            RadiusJointOuter = 0;
        }
        void GetCalTHA()
        { 
            //todo: Add the settings for head
        }
        void GetCalTHB()
        {
            
        }
        void GetCalFHA()
        {

        }
        void GetCalDHA()
        {
            HeightInner = Diameter * 0.225;
            HeightOuter = HeightInner + Delta;
            HeightFlat = Height - HeightInner;
            DiameterInner = Diameter;
            DiameterOuter = Diameter + 2*Delta;
            RadiusInner = Diameter;
            RadiusOuter = Diameter+Delta;
            RadiusJointInner = Diameter *0.15;
            RadiusJointOuter = RadiusJointInner +Delta;
        }
        void GetCalHHA()
        {
            HeightInner = Diameter / 2;
            HeightOuter = HeightInner + Delta;
            HeightFlat = Height - HeightInner;
            DiameterInner = Diameter;
            DiameterOuter = Diameter+2*Delta;
            ///Invalid parameter
            RadiusInner = 0;
            RadiusOuter = 0;
            RadiusJointInner = 0;
            RadiusJointOuter = 0;

        }
        void GetCalMD()
        {
            
        }
        #endregion

        public TopoShape CreateTopo(string name = "defalut")
        {
            switch (HeadType)
            {
                case HeadSetType.EHA:
                case HeadSetType.EHB:
                    return CreateEHAB();
                case HeadSetType.THA:
                    break;
                case HeadSetType.THB:
                    break;
                case HeadSetType.FHA:
                    break;
                case HeadSetType.DHA:
                    break;
                case HeadSetType.HHA:
                    break;
                case HeadSetType.MD:
                    break;
                default:
                    break;
            }
            return null;
        }

        private TopoShape CreateEHAB()
        {

            //XOZ平面创建内椭圆线
            TopoShape EInner = CurveBuilder.MakeEllipse(new(0, 0, this.HeightFlat), this.DiameterInner / 2, this.HeightInner / 2, GP.DX(), GP.DZ());
            //XOZ平面创建外椭圆线
            //TopoShape EOuter = CurveBuilder.MakeEllipse(new(0, 0, this.HeightFlat), this.DiameterInner/ 2, this.HeightInner / 2,GP.DX(),GP.DZ());
            //GElips eI = new GElips(GP.ZOX(), this.HeightInner / 2, this.DiameterInner / 2);
            //GElips eO = new GElips(GP.ZOX(), this.HeightOuter / 2, this.DiameterOuter / 2);
            //TopoShape eIArc = CurveBuilder.MakeArcOfEllipse(eI, 0.0, System.Math.PI/2);    //  1/4椭圆弧
            //TopoShape eOArc = CurveBuilder.MakeArcOfEllipse(eO, 0.0, System.Math.PI/2);    //  1/4椭圆弧
            //TopoShape eITop = FeatureTool.Revolve(eIArc, GP.OZ(), System.Math.PI*2);
            //TopoShape eOTop = FeatureTool.Revolve(eOArc, GP.OZ(), System.Math.PI* 2);
            //TopoShape eIbase = ProjectionTool.ProjectOnPlane(eITop,GP.Origin(),new(0,0,1),new(0,0,-1));
            //TopoShape eObase = ProjectionTool.ProjectOnPlane(eITop, GP.Origin(), new(0, 0, 1), new(0, 0, -1));
            //TopoShape eIBody = FeatureTool.Loft(eIbase,eITop,true);
            //TopoShape eOBody = FeatureTool.Loft(eObase, eOTop, true);
            //TopoShape temp = BooleanTool.Cut(eOBody, eIBody);


            ///创建内实体
            //创建内实体轮廓线
            double longInside = this.DiameterInner / 2; //椭圆长边半径 50
            double shortInside = this.DiameterInner / 4;   //椭圆短边 25
            double heightFlat = this.HeightFlat;       //封头直边长 10 = 35-25
            double heightInner = shortInside + heightFlat;
            TopoShape w0 = CurveBuilder.MakeLine(new(longInside, 0, 0), new(longInside, 0, heightFlat));
            GAx2 XOZ = new(new GPnt(0, 0, heightFlat), new GDir(0, -1, 0), new(1, 0, 0));
            GElips temp1 = new GElips(XOZ, longInside, shortInside);
            TopoShape w1 = CurveBuilder.MakeArcOfEllipse(temp1, 0, System.Math.PI / 2);
            TopoShape w2 = CurveBuilder.MakeLine(new(0, 0, heightInner), GP.Origin());
            TopoShape w3 = CurveBuilder.MakeLine(GP.Origin(), new GPnt(longInside, 0, 0));
            TopoShapeList wire = new TopoShapeList();
            wire.Add(w0);
            wire.Add(w1);
            wire.Add(w2);
            wire.Add(w3);
            TopoShape wire1 = CurveBuilder.MakeWire(wire); //wire创建面体
            //TopoShape wireReady = CurveBuilder.MakeClosedWire(wire1, true);//closedwire创建面体，不用也可以 ,9.3新版无效了
            TopoShape faceReady = CurveBuilder.MakePlanarFace(wire1);
            TopoShape bodyIn = FeatureTool.Revolve(faceReady, GP.OZ(), Math.PI * 2);
            //TopoShapeList topoT = bodyout.GetChildren(EnumTopoShapeType.Topo_SOLID);
            //int aa = topoT.Count;
            //int BB = aa;

            ///创建外实体
            longInside += this.Delta; //椭圆长边半径 50
            shortInside += this.Delta;   //椭圆短边 25
            heightInner += this.Delta;     //
            TopoShape w4 = CurveBuilder.MakeLine(new(longInside, 0, 0), new(longInside, 0, heightFlat));
            GElips temp2 = new GElips(XOZ, longInside, shortInside);
            TopoShape w5 = CurveBuilder.MakeArcOfEllipse(temp2, 0, System.Math.PI / 2);
            TopoShape w6 = CurveBuilder.MakeLine(new(0, 0, heightInner), GP.Origin());
            TopoShape w7 = CurveBuilder.MakeLine(GP.Origin(), new GPnt(longInside, 0, 0));
            TopoShapeList wireOut = new TopoShapeList();
            wireOut.Add(w4);
            wireOut.Add(w5);
            wireOut.Add(w6);
            wireOut.Add(w7);
            TopoShape wire2 = CurveBuilder.MakeWire(wireOut);
            TopoShape faceOutReady = CurveBuilder.MakePlanarFace(wire2);//wire创建面体
            TopoShape bodyOut = FeatureTool.Revolve(faceOutReady, GP.OZ(), Math.PI * 2);
            TopoShape outPart = BooleanTool.Cut(bodyOut, bodyIn);
            //MainWindow.Instance().MainVM.SetVesselTopo(vesselBody);
            this.InnerFace = FeatureTool.Revolve(w1, GP.OZ(), Math.PI * 2);
            this.OuterFace = FeatureTool.Revolve(w5, GP.OZ(), Math.PI * 2);

            return outPart;

        }

        private TopoShape CreateHHA()
        {
            //参考EHAB，长短轴设定一致
            TopoShape EInner = CurveBuilder.MakeEllipse(new(0, 0, this.HeightFlat), this.DiameterInner / 2, this.HeightInner / 2, GP.DX(), GP.DZ());
            ///创建内实体
            //创建内实体轮廓线
            double longInside = this.DiameterInner / 2; //椭圆长边半径 50
            double shortInside = this.DiameterInner / 2;   //
            double heightFlat = this.HeightFlat;       //封头直边长 10 = 35-25
            double heightInner = shortInside + heightFlat;
            TopoShape w0 = CurveBuilder.MakeLine(new(longInside, 0, 0), new(longInside, 0, heightFlat));
            GAx2 XOZ = new(new GPnt(0, 0, heightFlat), new GDir(0, -1, 0), new(1, 0, 0));
            GElips temp1 = new GElips(XOZ, longInside, shortInside);
            TopoShape w1 = CurveBuilder.MakeArcOfEllipse(temp1, 0, System.Math.PI / 2);
            TopoShape w2 = CurveBuilder.MakeLine(new(0, 0, heightInner), GP.Origin());
            TopoShape w3 = CurveBuilder.MakeLine(GP.Origin(), new GPnt(longInside, 0, 0));
            TopoShapeList wire = new TopoShapeList();
            wire.Add(w0);
            wire.Add(w1);
            wire.Add(w2);
            wire.Add(w3);
            TopoShape wire1 = CurveBuilder.MakeWire(wire); //wire创建面体
            //TopoShape wireReady = CurveBuilder.MakeClosedWire(wire1, true);//closedwire创建面体，不用也可以 ,9.3新版无效了
            TopoShape faceReady = CurveBuilder.MakePlanarFace(wire1);
            TopoShape bodyIn = FeatureTool.Revolve(faceReady, GP.OZ(), Math.PI * 2);
            //TopoShapeList topoT = bodyout.GetChildren(EnumTopoShapeType.Topo_SOLID);
            //int aa = topoT.Count;
            //int BB = aa;

            ///创建外实体
            longInside += this.Delta;
            shortInside += this.Delta;
            heightInner += this.Delta;
            //使用内参数，不做更改
            TopoShape w4 = CurveBuilder.MakeLine(new(longInside, 0, 0), new(longInside, 0, heightFlat));
            GElips temp2 = new GElips(XOZ, longInside, shortInside);
            TopoShape w5 = CurveBuilder.MakeArcOfEllipse(temp2, 0, System.Math.PI / 2);
            TopoShape w6 = CurveBuilder.MakeLine(new(0, 0, heightInner), GP.Origin());
            TopoShape w7 = CurveBuilder.MakeLine(GP.Origin(), new GPnt(longInside, 0, 0));
            TopoShapeList wireOut = new TopoShapeList();
            wireOut.Add(w4);
            wireOut.Add(w5);
            wireOut.Add(w6);
            wireOut.Add(w7);
            TopoShape wire2 = CurveBuilder.MakeWire(wireOut);
            TopoShape faceOutReady = CurveBuilder.MakePlanarFace(wire2);//wire创建面体
            TopoShape bodyOut = FeatureTool.Revolve(faceOutReady, GP.OZ(), Math.PI * 2);
            TopoShape outPart = BooleanTool.Cut(bodyOut, bodyIn);
            //MainWindow.Instance().MainVM.SetVesselTopo(vesselBody);
            ///创建用于计算的平面
            this.InnerFace = FeatureTool.Revolve(w1, GP.OZ(), Math.PI * 2);
            this.OuterFace = FeatureTool.Revolve(w5, GP.OZ(), Math.PI * 2);


            return outPart;



        }
        public enum HeadSetType
        {
            EHA =1, //标准椭圆封头
            EHB=2,  //
            THA=3,  //10%碟型封头
            THB=4,  //
            FHA=5,  //折边平底封头
            DHA=6,  //碟型封头
            HHA=7,  //半球封头
            MD=8,   //特殊碟型封头
        }

    }
}
