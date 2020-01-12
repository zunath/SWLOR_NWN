import * as React from 'react';
import * as dotnetify from 'dotnetify';
import { Link } from 'react-router-dom';

export default class Skills extends React.Component {
    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('SkillsViewModel', this);
        this.dispatch = state => this.vm.$dispatch(state);

        this.state = {
            SkillCategoryList: [],
            SkillList: [],
            SelectedCategoryID: 0,
            SelectedSkillID: 0,
            SelectedSkill: {}
        }

        this.handleChange = this.handleChange.bind(this);
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    handleChange(event) {
        const target = event.target;
        var value = target.type === 'checkbox' ? target.checked : target.value;
        const name = target.name;

        if (value === undefined)
            value = target.getAttribute('data-skillid');

        this.setState({
            [name]: value
        }, () => this.dispatch({ [name]: value }));
    }

    render() {
        return (
            <div>
                <div className="row">
                    <div className="col-12">
                        <div className="card border-primary mb-3 center">
                            <div className="card-body">
                                <h4 className="card-title">Skills</h4>

                                <p>
                                    Star Wars: LOR introduces skill-based gameplay to NWN. Instead of being held back by restrictive classes, you define your character by playing it.
                                    <br />
                                    Want to be a Jedi Sage? Practice your Light-Side force abilities, like Force Healing.
                                    <br />
                                    How about a Sith Sorcerer? Hone your Dark-Side force abilities, like Force Lightning.
                                    <br />
                                    Or perhaps you'd rather support the war efforts by crafting items and equipment to sell to other players.
                                    <br />
                                    Specialize in a handful of skills or become a jack-of-all-trades. What you choose is entirely up to you!
                                </p>
                            </div>
                        </div>
                    </div>
                </div>

                <div className="row">
                    <div className="col-3">
                        <h3 className="center">Skill List</h3>
                        <hr />

                        <div className="card border-primary mb-3">
                            <div className="card-body">
                                <div className="row">


                                    <div className="col">
                                        <select className="form-control"
                                            name="SelectedCategoryID"
                                            onChange={this.handleChange}
                                            value={this.state.SelectedCategoryID} >
                                            {this.state.SkillCategoryList.map(obj =>
                                                <option key={obj.SkillCategoryID} value={obj.SkillCategoryID}>
                                                    {obj.Name}
                                                </option>
                                            )}
                                        </select>
                                    </div>

                                </div>

                                <div className="row">&nbsp;</div>

                                <div className="list-group">
                                    {this.state.SkillList.map(obj =>
                                        <Link key={obj.SkillID}
                                            className={this.state.SelectedSkillID === obj.SkillID ? 'list-group-item list-group-item-action active' : 'list-group-item list-group-item-action'}
                                            to="#"
                                            onClick={this.handleChange}
                                            name="SelectedSkillID"
                                            data-skillid={obj.SkillID}>
                                            {obj.Name}
                                        </Link>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="col offset-1">
                        <h3 className="center">Details</h3>
                        <hr />


                        <div className="card border-primary mb-3">
                            <div className="card-body">
                                <div className="row">
                                    <div className="col-2">
                                        <b>Name:</b>
                                    </div>
                                    <div className="col">
                                        {this.state.SelectedSkill.Name}
                                    </div>

                                </div>
                                <div className="row">&nbsp;</div>
                                <div className="row">
                                    <div className="col-2">
                                        <b>Description:</b>
                                    </div>
                                    <div className="col">
                                        {this.state.SelectedSkill.Description}
                                    </div>
                                </div>

                                <div className="row">&nbsp;</div>
                                <div className="row">
                                    <div className="col-2">
                                        <b>Ranks:</b>
                                    </div>
                                    <div className="col-2">
                                        {this.state.SelectedSkill.MaxRank}
                                    </div>
                                    <div className="col-2">
                                        <b>Primary:</b>
                                    </div>
                                    <div className="col-2">
                                        {this.state.SelectedSkill.PrimaryName}
                                    </div>
                                    <div className="col-2">
                                        <b>Secondary:</b>
                                    </div>
                                    <div className="col-2 pl-4">
                                        {this.state.SelectedSkill.SecondaryName}
                                    </div>
                                    <div className="col-2 offset-4">
                                        <b>Tertiary:</b>
                                    </div>
                                    <div className="col-1">
                                        {this.state.SelectedSkill.TertiaryName}
                                    </div>
                                </div>

                            </div>
                        </div>
                    </div>

                </div>

                <div className="row">
                    <div className="col-3">
                        <a className="btn btn-primary btn-block" href="/DataExport/Skills">Export Data</a>
                    </div>
                </div>


            </div>
        );



    }
}
