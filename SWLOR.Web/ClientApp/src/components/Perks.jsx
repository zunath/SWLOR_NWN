import * as React from 'react';
import * as dotnetify from 'dotnetify';
import { Link } from 'react-router-dom';

export default class Perks extends React.Component {
    constructor(props) {
        super(props);
        this.vm = dotnetify.react.connect('PerksViewModel', this);
        this.dispatch = state => this.vm.$dispatch(state);

        this.state = {
            ActivePerkLevel: 1
        };

        this.handleChange = this.handleChange.bind(this);
        this.toggle = this.toggle.bind(this);
    }

    componentWillUnmount() {
        this.vm.$destroy();
    }

    handleChangeCategory() {
        this.dispatch({
            SelectedCategory: this.state.SelectedCategory,
            ActivePerkLevel: 1
        });
    }

    handleChange(event) {
        const target = event.target;
        var value = target.type === 'checkbox' ? target.checked : target.value;
        const name = target.name;

        if (value === undefined)
            value = target.getAttribute('data-perkid');

        this.setState({
            [name]: value,
            ActivePerkLevel: 1
        }, () => this.dispatch({ [name]: value }));
    }

    toggle(event) {
        const level = event.target.getAttribute('data-perklevel');
        if (this.state.ActivePerkLevel !== level) {
            this.setState({
                ActivePerkLevel: level
            });
        }
    }

    render() {
        return (
            <div>
                {this.state.IsInitialized && <div>

                    <div className="row">
                        <div className="col-12">
                            <div className="card border-primary mb-3 center">
                                <div className="card-body">
                                    <h4 className="card-title">Perks</h4>

                                    <p>
                                        Perks cover a wide range of upgrades available to your character.
                                    <br />
                                        Every time you gain a skill level, you also gain 1 skill point (SP).
                                    <br />
                                        These SP can be spent to purchase everything from stat upgrades like HP and FP to learning new combat and force abilities.
                                    <br />
                                        Most Perks have multiple ranks so you can decide how deeply specialized you want to be.
                                    <br />
                                        Take a look at the available Perks below. New ones are being added all the time!
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>



                    <div className="row">
                        <div className="col-3">
                            <h3 className="center">Perk List</h3>
                            <hr />


                            <div className="card border-primary mb-3">
                                <div className="card-body">
                                    <div className="row">


                                        <div className="col">
                                            <select className="form-control"
                                                name="SelectedCategoryID"
                                                onChange={this.handleChange}
                                                value={this.state.SelectedCategoryID} >
                                                {this.state.PerkCategoryList.map(obj =>
                                                    <option key={'perkcategory_' + obj.PerkCategoryID} value={obj.PerkCategoryID}>
                                                        {obj.Name}
                                                    </option>
                                                )}
                                            </select>
                                        </div>

                                    </div>

                                    <div className="row">&nbsp;</div>

                                    <div className="list-group">
                                        {this.state.PerkList.map(obj =>
                                            <Link key={'perklist_perkid_' + obj.PerkID}
                                                className={this.state.SelectedPerkID === obj.PerkID ? 'list-group-item list-group-item-action active' : 'list-group-item list-group-item-action'}
                                                to="#"
                                                onClick={this.handleChange}
                                                name="SelectedPerkID"
                                                data-perkid={obj.PerkID}>
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
                                            {this.state.SelectedPerk.Name}
                                        </div>

                                    </div>
                                    <div className="row">&nbsp;</div>

                                    {this.state.SelectedPerk.ExecutionTypeID > 0 &&
                                        <div className="row">
                                            <div className="col-3">
                                                <b>Type:</b>
                                            </div>
                                            <div className="col">
                                                <p>
                                                    {this.state.SelectedPerk.ExecutionType.Name}
                                                </p>

                                            </div>
                                        </div>
                                    }

                                    <div className="row">
                                        <div className="col-2">
                                            <b>Description:</b>
                                        </div>
                                        <div className="col">
                                            <p>
                                                {this.state.SelectedPerk.Description}
                                            </p>

                                        </div>
                                    </div>

                                    <div className="row">&nbsp;</div>
                                    <ul className="nav nav-tabs"
                                        role="tablist">
                                        {this.state.SelectedPerk.PerkLevels.map(perkLevel =>
                                            <li className="nav-item"
                                                key={'tab_' + perkLevel.PerkLevelID}>
                                                <Link
                                                    className={this.state.ActivePerkLevel === perkLevel.Level ? 'nav-link active' : 'nav-link'}
                                                    data-toggle="tab"
                                                    data-perklevel={perkLevel.Level}
                                                    onClick={this.toggle}
                                                    to={'#nav-rank' + perkLevel.Level} role="tab">Lvl {perkLevel.Level}</Link>
                                            </li>
                                        )}
                                    </ul>

                                    <div className="tab-content">
                                        {this.state.SelectedPerk.PerkLevels.map(perkLevel =>
                                            <div className={this.state.ActivePerkLevel == perkLevel.Level ? 'tab-pane active' : 'tab-pane'}
                                                id={'nav-rank' + perkLevel.Level}
                                                role="tabpanel"
                                                key={'tabcontent_' + perkLevel.PerkLevelID}>
                                                <div className="row">&nbsp;</div>
                                                <div className="row">
                                                    <div className="col-2">
                                                        <b>Bonus:</b>
                                                    </div>
                                                    <div className="col-10">
                                                        {perkLevel.Description}
                                                    </div>
                                                </div>
                                                <div className="row">&nbsp;</div>
                                                <div className="row">
                                                    <div className="col-2">
                                                        <b>Price:</b>
                                                    </div>
                                                    <div className="col-10">
                                                        {perkLevel.Price} SP
                                            </div>
                                                </div>
                                                <div className="row">&nbsp;</div>

                                                {perkLevel.SkillRequirements.length > 0 &&
                                                    <div className="row">
                                                        <div className="col">
                                                            <b>Requirements:</b>
                                                        </div>
                                                    </div>
                                                }

                                                <div className="row">&nbsp;</div>

                                                {perkLevel.SkillRequirements.map(req =>
                                                    <div className="row"
                                                        key={'perklevel_' + req.PerkLevelSkillRequirementID}>
                                                        <div className="col">
                                                            {req.SkillName} rank {req.RequiredRank}
                                                        </div>
                                                    </div>
                                                )}
                                            </div>
                                        )}

                                    </div>


                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="row">
                        <div className="col-3">
                            <a className="btn btn-primary btn-block" href="/DataExport/Perks">Export Data</a>
                        </div>
                    </div>
                </div>}


            </div>
        );
    }
}
