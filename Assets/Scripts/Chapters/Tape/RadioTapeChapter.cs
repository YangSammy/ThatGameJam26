namespace MemoryWhitebox
{
    // Current radio chapter: inspect, replace, load and play an intact tape.
    public sealed class RadioTapeChapter : Chapter
    {
        public readonly TapeInspectionState Tape=new TapeInspectionState();
        public override string Help => "按住左键拿起磁带，拖到卡槽后松手装入；槽外松手留在手中，右键放回。\n手持时指向轮轴，按住左键并滚动检查；右轮轴正滚向前，左轮轴正滚倒带。按住外壳可再次拖动。发现断头后放回换带。\n沿用收音机装带与播放动画。完整检查过的好带开始播放后完成本阶段。E 安全检视只改变展示副本；Esc 返回书房并保留本次检查进度。";
        public override void Draw(){}
        public void VerifyPlayback(){if(Tape.Selected>=0 && !Tape.Candidates[Tape.Selected].Broken && Tape.Candidates[Tape.Selected].Scanned>=TapeInspectionState.Samples)Finish();}
    }
    public sealed class RadioTapeWorld : WorldChapter3D
    {
        readonly HouseRadioDeck deck;
        public RadioTapeWorld(HouseRadioDeck deck):base(deck.Chapter,deck.gameObject,deck.view)
        {
            this.deck=deck;ReadViewAnchor();
        }
        public void ReadViewAnchor()
        {
            var anchor=deck.operationView;
            var pose=anchor!=null?anchor.transform:deck.view.transform;
            Eye=pose.position;Look=Eye+pose.forward;Fov=anchor!=null?anchor.fieldOfView:deck.view.fieldOfView;
        }
    }
}
